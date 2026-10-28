// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.SplitMigrate;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.StateMachine.SplitMigrate.Utils;

namespace SnapCd.Server.Core.StateMachine.SplitMigrate;

public partial class SplitMigrateStateMachine
{
    public Request<SplitMigrateSaga, SplitCancelKillRequested, DummySplitCancelKillCompleted> CancelKillRequested { get; } = null!;
    public Event<SplitCancelKillCompleted> CancelKillCompleted { get; } = null!;


    public Event<CancelStateMigrationJobRequested> CancelStateMigrationJobRequested { get; } = null!;

    public State CancellingImmediateKill { get; } = null!;
    public State CancellingAfterCurrent { get; } = null!;
    public State Cancelled { get; } = null!;

    private static readonly TimeSpan CancelRequestTimeout = TimeSpan.FromSeconds(90);

    private void Configure_Cancel()
    {
        Event(() => CancelStateMigrationJobRequested, x => x
            .CorrelateById(m => m.Message.CorrelationId)
            .OnMissingInstance(m => m.ExecuteAsync(context => FinalizeWithoutSaga(context, context.Message.CorrelationId, context.Message.OrganizationId, nameof(CancelStateMigrationJobRequested)))));

        Request(() => CancelKillRequested, x => x.KillCancellationRequestId, o => { o.Timeout = CancelRequestTimeout; });
        Event(() => CancelKillCompleted, x => x
            .CorrelateById(y => y.Message.CorrelationId)
            .OnMissingInstance(m => m.ExecuteAsync(context => FinalizeWithoutSaga(context, context.Message.CorrelationId, context.Message.OrganizationId, nameof(SplitCancelKillCompleted)))));

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
                SplitMigrateExtensionMethods.ThenSplitCancelForced(When(CancelStateMigrationJobRequested, TimeoutIsOverdue), _logger, Cancelled),
                Ignore(CancelStateMigrationJobRequested),
                SplitMigrateExtensionMethods.ThenSplitCancelTimedOut<SplitCancelKillRequested>(When(CancelKillRequested.TimeoutExpired), _logger, Cancelled)
            ]);
        During(CancellingAfterCurrent,
            [
                .. CancelHandlers(),
                SplitMigrateExtensionMethods.ThenSplitCancelForced(When(CancelStateMigrationJobRequested, TimeoutIsOverdue), _logger, Cancelled),
                Ignore(CancelStateMigrationJobRequested)
            ]);

        During(Cancelled,
            Ignore(CancelStateMigrationJobRequested),
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
    private static bool TimeoutIsOverdue(BehaviorContext<SplitMigrateSaga, CancelStateMigrationJobRequested> context) =>
        context.Saga.WaitingSince is { } since && DateTime.UtcNow - since > CancelRequestTimeout + TimeSpan.FromSeconds(15);

    /// <summary>
    /// Closes the job row when its saga is gone. Without this a cancel is silently swallowed and
    /// the row stays Running until the orphan sweep finds it, which reports it as abandoned rather
    /// than cancelled.
    /// </summary>
    private async Task FinalizeWithoutSaga(ConsumeContext context, Guid jobId, Guid organizationId, string eventName)
    {
        var serviceProvider = PipeExtensions.GetPayload<IServiceProvider>(context);
        var repository = serviceProvider.GetRequiredService<StateMigrationJobRepository>();
        var publishEndpoint = serviceProvider.GetRequiredService<IPublishEndpoint>();

        LoggerExtensions.LogWarning(_logger, "Saga missing for {EventType} on manual job {JobId}, finalizing directly", eventName, jobId);

        // Published, so this reaches every saga: an id belonging to an ordinary job is not ours to
        // finalize. The repository throws rather than returning null.
        StateMigrationJob job;
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
        SplitMigrateExtensionMethods.ThenSplitCancelled<SelectRunnerInstanceCompleted>(When(SelectRunnerInstanceCompleted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SplitGetModuleCompleted>(When(SplitGetModuleCompleted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SplitInitCompleted>(When(SplitInitCompleted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SplitValidateCompleted>(When(SplitValidateCompleted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SplitPlanCompleted>(When(SplitPlanCompleted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SplitPlanEmptyVerifyCompleted>(When(SplitPlanEmptyVerifyCompleted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SplitRefactorValidateCompleted>(When(SplitRefactorValidateCompleted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SplitRefactorDiffCompleted>(When(SplitRefactorDiffCompleted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SplitMigrateMapCompleted>(When(SplitMigrateMapCompleted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SplitMigrateProveCompleted>(When(SplitMigrateProveCompleted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SplitMigrateRunCompleted>(When(SplitMigrateRunCompleted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SplitMigrateVerifyCompleted>(When(SplitMigrateVerifyCompleted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SplitCancelKillCompleted>(When(CancelKillCompleted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelledAfterFault<SelectRunnerInstanceFaulted>(When(SelectRunnerInstanceFaulted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SelectRunnerInstanceCancelled>(When(SelectRunnerInstanceCancelled), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelledAfterFault<SplitGetModuleFaulted>(When(SplitGetModuleFaulted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SplitGetModuleCancelled>(When(SplitGetModuleCancelled), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelledAfterFault<SplitInitFaulted>(When(SplitInitFaulted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SplitInitCancelled>(When(SplitInitCancelled), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelledAfterFault<SplitValidateFaulted>(When(SplitValidateFaulted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SplitValidateCancelled>(When(SplitValidateCancelled), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelledAfterFault<SplitPlanFaulted>(When(SplitPlanFaulted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SplitPlanCancelled>(When(SplitPlanCancelled), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelledAfterFault<SplitPlanEmptyVerifyFaulted>(When(SplitPlanEmptyVerifyFaulted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SplitPlanEmptyVerifyCancelled>(When(SplitPlanEmptyVerifyCancelled), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelledAfterFault<SplitRefactorValidateFaulted>(When(SplitRefactorValidateFaulted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SplitRefactorValidateCancelled>(When(SplitRefactorValidateCancelled), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelledAfterFault<SplitRefactorDiffFaulted>(When(SplitRefactorDiffFaulted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SplitRefactorDiffCancelled>(When(SplitRefactorDiffCancelled), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelledAfterFault<SplitMigrateMapFaulted>(When(SplitMigrateMapFaulted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SplitMigrateMapCancelled>(When(SplitMigrateMapCancelled), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelledAfterFault<SplitMigrateProveFaulted>(When(SplitMigrateProveFaulted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SplitMigrateProveCancelled>(When(SplitMigrateProveCancelled), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelledAfterFault<SplitMigrateRunFaulted>(When(SplitMigrateRunFaulted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SplitMigrateRunCancelled>(When(SplitMigrateRunCancelled), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelledAfterFault<SplitMigrateVerifyFaulted>(When(SplitMigrateVerifyFaulted), Cancelled),
        SplitMigrateExtensionMethods.ThenSplitCancelled<SplitMigrateVerifyCancelled>(When(SplitMigrateVerifyCancelled), Cancelled),
        Ignore(RunnerReconnectedEvent),
        Ignore(HeartbeatScheduled.Received),
        Ignore(HeartbeatRequested.Completed),
        Ignore(HeartbeatRequested.Completed2)
    ];
}
