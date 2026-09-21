// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using MassTransit.Contracts;
using SnapCd.Contracts;
using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.StateMachine.ManualJobs.Finalization;

namespace SnapCd.Server.Core.StateMachine.SplitMigrate;

/// <summary>
/// Terminal transitions for a split job. Parallel to JobExtensionMethods rather than shared,
/// because those select finalization activities that write ModuleJobs and carry deployment
/// vocabulary. Keeping them apart means a change made for a deployment reason cannot alter how a
/// manual job closes — which matters more here, since a manual job left open blocks every future
/// manual job on the module through the filtered unique index.
/// </summary>
public static class SplitMigrateExtensionMethods
{
    public static EventActivityBinder<SplitMigrateSaga, TFaulted> ThenSplitFaulted<TFaulted>(this
        EventActivityBinder<SplitMigrateSaga, TFaulted> binder, State failed, ILogger logger)
        where TFaulted : StepResponseBase
    {
        return binder
            .Then(x => logger.LogInformation(
                "SplitMigrate failed for Module {ModuleId}, job {JobId}", x.Saga.ModuleId, x.Saga.CorrelationId))
            .Publish(context => new SplitMigrateFailed
            {
                ModuleId = context.Saga.ModuleId,
                OrganizationId = context.Saga.OrganizationId,
                ModuleJobId = context.Saga.CorrelationId
            })
            .Activity(x => x.OfType<FailManualModuleJobActivity<SplitMigrateSaga, TFaulted>>())
            .TransitionTo(failed)
            .Finalize();
    }

    public static EventActivityBinder<SplitMigrateSaga, TCancelled> ThenSplitCancelled<TCancelled>(this
        EventActivityBinder<SplitMigrateSaga, TCancelled> binder, State cancelled)
        where TCancelled : StepResponseBase
    {
        return binder
            .Publish(context => new SplitMigrateCancelled
            {
                ModuleId = context.Saga.ModuleId,
                OrganizationId = context.Saga.OrganizationId,
                ModuleJobId = context.Saga.CorrelationId
            })
            .Activity(x => x.OfType<CancelManualModuleJobActivity<SplitMigrateSaga, TCancelled>>())
            .TransitionTo(cancelled)
            .Finalize();
    }

    public static EventActivityBinder<SplitMigrateSaga, TCompleted> ThenSplitCompleted<TCompleted>(this
        EventActivityBinder<SplitMigrateSaga, TCompleted> binder, State completed)
        where TCompleted : StepResponseBase
    {
        return binder
            .Publish(context => new SplitMigrateCompleted
            {
                ModuleId = context.Saga.ModuleId,
                OrganizationId = context.Saga.OrganizationId,
                ModuleJobId = context.Saga.CorrelationId
            })
            .Activity(x => x.OfType<CompleteManualModuleJobActivity<SplitMigrateSaga, TCompleted>>())
            .TransitionTo(completed)
            .Finalize();
    }

    /// <summary>
    /// A cancel clicked again after its timeout should already have fired. The timeout was lost, so
    /// nothing else will end this job: close it here rather than leaving the saga cancelling forever.
    /// </summary>
    public static EventActivityBinder<SplitMigrateSaga, CancelManualModuleJobRequested> ThenSplitCancelForced(this
        EventActivityBinder<SplitMigrateSaga, CancelManualModuleJobRequested> binder, ILogger logger, State cancelled)
    {
        return binder
            .Then(context => logger.LogWarning(
                "SplitMigrate: cancel re-requested for job {JobId} after its timeout was due; forcing it closed",
                context.Saga.CorrelationId))
            .Publish(context => new SplitMigrateCancelled
            {
                ModuleId = context.Saga.ModuleId,
                OrganizationId = context.Saga.OrganizationId,
                ModuleJobId = context.Saga.CorrelationId,
                CancellationReason = CancellationReason.UserRequested
            })
            .Activity(x => x.OfType<CancelManualModuleJobActivity<SplitMigrateSaga, CancelManualModuleJobRequested>>())
            .TransitionTo(cancelled)
            .Finalize();
    }

    /// <summary>
    /// A cancel request the runner never answers. Ends the job rather than leaving the saga in
    /// Cancelling: the commonest cause is that no step had been dispatched, so there was nothing
    /// on the runner to cancel.
    /// </summary>
    public static EventActivityBinder<SplitMigrateSaga, RequestTimeoutExpired<TRequest>> ThenSplitCancelTimedOut<TRequest>(this
        EventActivityBinder<SplitMigrateSaga, RequestTimeoutExpired<TRequest>> binder, ILogger logger, State cancelled)
        where TRequest : class
    {
        return binder
            .Then(context => logger.LogInformation(
                "SplitMigrate: cancel request timed out for job {JobId}; ending it anyway", context.Saga.CorrelationId))
            .Publish(context => new SplitMigrateCancelled
            {
                ModuleId = context.Saga.ModuleId,
                OrganizationId = context.Saga.OrganizationId,
                ModuleJobId = context.Saga.CorrelationId,
                CancellationReason = CancellationReason.UserRequested
            })
            .Activity(x => x.OfType<CancelManualModuleJobActivity<SplitMigrateSaga, RequestTimeoutExpired<TRequest>>>())
            .TransitionTo(cancelled)
            .Finalize();
    }

    public static EventActivityBinder<SplitMigrateSaga, HeartbeatFailed> ThenSplitTimedOut(this
        EventActivityBinder<SplitMigrateSaga, HeartbeatFailed> binder, State failed)
    {
        return binder
            .Publish(context => new SplitMigrateFailed
            {
                ModuleId = context.Saga.ModuleId,
                ModuleJobId = context.Saga.CorrelationId,
                OrganizationId = context.Saga.OrganizationId
            })
            .Activity(x => x.OfType<FailManualModuleJobActivity<SplitMigrateSaga, HeartbeatFailed>>())
            .TransitionTo(failed)
            .Finalize();
    }
}
