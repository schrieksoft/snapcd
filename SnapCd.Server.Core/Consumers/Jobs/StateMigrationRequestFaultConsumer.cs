// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

namespace SnapCd.Server.Core.Consumers.Jobs;

/// <summary>
/// Fails a manual job whose request could not be handled.
///
/// The retry filter sits outside the saga, so an exception thrown while a job is being dispatched
/// rolls the saga back and never reaches the state machine's faulted arms: those answer a fault
/// the runner reports about a step, which is a different thing. Without this the job row keeps the
/// status it was given when it was created, and the filtered unique index on Running then blocks
/// every later manual job on that Module.
/// </summary>
public class StateMigrationRequestFaultConsumer :
    IConsumer<Fault<MoveJobRequested>>,
    IConsumer<Fault<ImportJobRequested>>,
    IConsumer<Fault<RemoveJobRequested>>
{
    private readonly StateMigrationJobRepository _jobs;
    private readonly ILogger<StateMigrationRequestFaultConsumer> _logger;

    public StateMigrationRequestFaultConsumer(
        StateMigrationJobRepository jobs,
        ILogger<StateMigrationRequestFaultConsumer> logger)
    {
        _jobs = jobs;
        _logger = logger;
    }

    public Task Consume(ConsumeContext<Fault<MoveJobRequested>> context) =>
        Fail(context.Message.Message.CorrelationId, context.Message.Message.OrganizationId,
            StateMigrationTypes.Move, context.Message.Exceptions);

    public Task Consume(ConsumeContext<Fault<ImportJobRequested>> context) =>
        Fail(context.Message.Message.CorrelationId, context.Message.Message.OrganizationId,
            StateMigrationTypes.Import, context.Message.Exceptions);

    public Task Consume(ConsumeContext<Fault<RemoveJobRequested>> context) =>
        Fail(context.Message.Message.CorrelationId, context.Message.Message.OrganizationId,
            StateMigrationTypes.Remove, context.Message.Exceptions);

    private async Task Fail(Guid jobId, Guid organizationId, string jobType, ExceptionInfo[]? exceptions)
    {
        var reason = exceptions is { Length: > 0 }
            ? $"{exceptions[0].ExceptionType}: {exceptions[0].Message}"
            : "No exception was reported with the fault.";

        _logger.LogError(
            "Manual {JobType} job {JobId} could not be started: {Reason}", jobType, jobId, reason);

        await _jobs.FinalizeWithServerError(
            jobId,
            organizationId,
            DateTimeOffset.UtcNow,
            null,
            "This job could not be started.",
            reason);
    }
}
