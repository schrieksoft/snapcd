// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Events.Steps.ManualJobs;
using SnapCd.Server.Core.Events.Steps.Transfer;

namespace SnapCd.Server.Core.Hubs.Handlers.Transfers;

/// <summary>
/// Turns a runner's transfer reply into the event the Module's saga is waiting for. Every reply
/// names the Module, because two of them run under one job.
/// </summary>
public class TransferStepHandler
{
    private readonly ILogger<TransferStepHandler> _logger;
    private readonly IBus _bus;

    public TransferStepHandler(ILogger<TransferStepHandler> logger, IBus bus)
    {
        _logger = logger;
        _bus = bus;
    }

    /// <summary>
    /// For the steps a transfer shares with an ordinary job, whose replies name only the job. The
    /// job id identifies the saga on its own; the Module is read from the saga.
    /// </summary>
    public Task Complete<TCompleted>(Guid jobId, Guid organizationId, Action<TCompleted>? fill = null)
        where TCompleted : ManualStepResponseBase, new()
        => Complete(jobId, Guid.Empty, organizationId, fill);

    public Task Fault<TFaulted>(Guid jobId, Guid organizationId, string? errorMessage, string? stackTrace)
        where TFaulted : ManualStepFaultedBase, new()
        => Fault<TFaulted>(jobId, Guid.Empty, organizationId, errorMessage, stackTrace);

    public async Task Complete<TCompleted>(Guid jobId, Guid moduleId, Guid organizationId, Action<TCompleted>? fill = null)
        where TCompleted : ManualStepResponseBase, new()
    {
        _logger.LogDebug("Runner completed {Step} for Module {ModuleId} of job {JobId}",
            typeof(TCompleted).Name, moduleId, jobId);

        var completed = new TCompleted
        {
            CorrelationId = jobId,
            OrganizationId = organizationId,
            ModuleId = moduleId
        };

        fill?.Invoke(completed);

        await _bus.Publish(completed);
    }

    public async Task Fault<TFaulted>(
        Guid jobId, Guid moduleId, Guid organizationId, string? errorMessage, string? stackTrace)
        where TFaulted : ManualStepFaultedBase, new()
    {
        _logger.LogError("Runner faulted {Step} for Module {ModuleId} of job {JobId}: {ErrorMessage}",
            typeof(TFaulted).Name, moduleId, jobId, errorMessage);

        await _bus.Publish(new TFaulted
        {
            CorrelationId = jobId,
            OrganizationId = organizationId,
            ModuleId = moduleId,
            ErrorMessage = errorMessage,
            StackTrace = stackTrace
        });
    }
}
