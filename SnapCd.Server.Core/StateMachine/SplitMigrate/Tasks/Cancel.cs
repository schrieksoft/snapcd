// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Contracts;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Events.Steps.SplitMigrate;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;

namespace SnapCd.Server.Core.StateMachine.SplitMigrate;

public partial class SplitMigrateStateMachine
{
    public Request<SplitMigrateSaga, CancelKillRequested, DummyCancelKillCompleted> CancelKillRequested { get; } = null!;
    public Event<CancelKillCompleted> CancelKillCompleted { get; } = null!;

    public Request<SplitMigrateSaga, CancelGracefulRequested, DummyCancelGracefulCompleted> CancelGracefulRequested { get; } = null!;
    public Event<CancelGracefulCompleted> CancelGracefulCompleted { get; } = null!;

    public Event<CancelManualModuleJobRequested> CancelManualModuleJobRequested { get; } = null!;

    public State CancellingImmediateKill { get; } = null!;
    public State CancellingImmediateGraceful { get; } = null!;
    public State CancellingAfterCurrent { get; } = null!;
    public State Cancelled { get; } = null!;

    private static readonly TimeSpan CancelRequestTimeout = TimeSpan.FromSeconds(90);

    private void Configure_Cancel()
    {
        Event(() => CancelManualModuleJobRequested, x => x
            .CorrelateById(m => m.Message.CorrelationId)
            .OnMissingInstance(m => m.ExecuteAsync(context => FinalizeWithoutSaga(context, context.Message.CorrelationId, context.Message.OrganizationId, nameof(CancelManualModuleJobRequested)))));

        Request(() => CancelKillRequested, x => x.KillCancellationRequestId, o => { o.Timeout = CancelRequestTimeout; });
        Event(() => CancelKillCompleted, x => x
            .CorrelateById(y => y.Message.CorrelationId)
            .OnMissingInstance(m => m.ExecuteAsync(context => FinalizeWithoutSaga(context, context.Message.CorrelationId, context.Message.OrganizationId, nameof(CancelKillCompleted)))));

        Request(() => CancelGracefulRequested, x => x.GracefulCancellationRequestId, o => { o.Timeout = CancelRequestTimeout; });
        Event(() => CancelGracefulCompleted, x => x
            .CorrelateById(y => y.Message.CorrelationId)
            .OnMissingInstance(m => m.ExecuteAsync(context => FinalizeWithoutSaga(context, context.Message.CorrelationId, context.Message.OrganizationId, nameof(CancelGracefulCompleted)))));

        // A step that reports back while cancelling ends the job rather than continuing the chain.
        // The timeouts matter most when no step was ever dispatched: the runner has nothing to
        // kill and never answers, so without these the saga waits in Cancelling forever.
        // Cancelling again never re-issues the request: that would restart the clock, and repeated
        // clicks would push the deadline out indefinitely while orphaning each previous timeout. A
        // click once the timeout is overdue is instead evidence that it was lost, and forces the
        // job closed. Inside the window the click does nothing, because the timeout is still coming.
        During(CancellingImmediateKill,
            [
                .. CancelHandlers(),
                When(CancelManualModuleJobRequested, TimeoutIsOverdue).ThenSplitCancelForced(_logger, Cancelled),
                Ignore(CancelManualModuleJobRequested),
                When(CancelKillRequested.TimeoutExpired).ThenSplitCancelTimedOut(_logger, Cancelled)
            ]);
        During(CancellingImmediateGraceful,
            [
                .. CancelHandlers(),
                When(CancelManualModuleJobRequested, TimeoutIsOverdue).ThenSplitCancelForced(_logger, Cancelled),
                Ignore(CancelManualModuleJobRequested),
                When(CancelGracefulRequested.TimeoutExpired).ThenSplitCancelTimedOut(_logger, Cancelled)
            ]);
        During(CancellingAfterCurrent,
            [
                .. CancelHandlers(),
                When(CancelManualModuleJobRequested, TimeoutIsOverdue).ThenSplitCancelForced(_logger, Cancelled),
                Ignore(CancelManualModuleJobRequested)
            ]);

        During(Cancelled,
            Ignore(CancelManualModuleJobRequested),
            Ignore(RunnerReconnectedEvent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );
    }

    /// <summary>
    /// The cancel request this saga is waiting on should have timed out by now. A margin over the
    /// request's own timeout keeps a merely-late timeout from being treated as a lost one.
    /// </summary>
    private static bool TimeoutIsOverdue(BehaviorContext<SplitMigrateSaga, CancelManualModuleJobRequested> context) =>
        context.Saga.WaitingSince is { } since && DateTime.UtcNow - since > CancelRequestTimeout + TimeSpan.FromSeconds(15);

    /// <summary>
    /// Closes the job row when its saga is gone. Without this a cancel is silently swallowed and
    /// the row stays Running until the orphan sweep finds it, which reports it as abandoned rather
    /// than cancelled.
    /// </summary>
    private async Task FinalizeWithoutSaga(ConsumeContext context, Guid jobId, Guid organizationId, string eventName)
    {
        var serviceProvider = PipeExtensions.GetPayload<IServiceProvider>(context);
        var repository = serviceProvider.GetRequiredService<ManualModuleJobRepository>();
        var publishEndpoint = serviceProvider.GetRequiredService<IPublishEndpoint>();

        _logger.LogWarning("Saga missing for {EventType} on manual job {JobId}, finalizing directly", eventName, jobId);

        // Published, so this reaches every saga: an id belonging to an ordinary job is not ours to
        // finalize. The repository throws rather than returning null.
        ManualModuleJob job;
        try
        {
            job = await repository.Get(jobId, organizationId);
        }
        catch (EntityNotFoundException)
        {
            return;
        }

        await publishEndpoint.Publish(new SplitMigrateCancelled
        {
            ModuleId = job.ModuleId,
            OrganizationId = organizationId,
            ModuleJobId = jobId,
            CancellationReason = CancellationReason.UserRequested
        });

        await repository.Finalize(jobId, organizationId, ExecutionStatus.Cancelled, DateTimeOffset.UtcNow);
    }

    private EventActivities<SplitMigrateSaga>[] CancelHandlers() =>
    [
        When(SelectRunnerInstanceCompleted).ThenSplitCancelled(Cancelled),
        When(SplitGetModuleCompleted).ThenSplitCancelled(Cancelled),
        When(SplitInitCompleted).ThenSplitCancelled(Cancelled),
        When(SplitValidateCompleted).ThenSplitCancelled(Cancelled),
        When(SplitPlanCompleted).ThenSplitCancelled(Cancelled),
        When(PlanEmptyVerifyCompleted).ThenSplitCancelled(Cancelled),
        When(RefactorValidateCompleted).ThenSplitCancelled(Cancelled),
        When(RefactorDiffCompleted).ThenSplitCancelled(Cancelled),
        When(MigrateMapCompleted).ThenSplitCancelled(Cancelled),
        When(MigrateProveCompleted).ThenSplitCancelled(Cancelled),
        When(MigrateRunCompleted).ThenSplitCancelled(Cancelled),
        When(MigrateVerifyCompleted).ThenSplitCancelled(Cancelled),
        When(CancelKillCompleted).ThenSplitCancelled(Cancelled),
        When(CancelGracefulCompleted).ThenSplitCancelled(Cancelled),
        Ignore(RunnerReconnectedEvent),
        Ignore(HeartbeatScheduled.Received),
        Ignore(HeartbeatRequested.Completed),
        Ignore(HeartbeatRequested.Completed2)
    ];
}
