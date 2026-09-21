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

namespace SnapCd.Server.Core.StateMachine.SplitMonolith;

/// <summary>
/// Terminal transitions for a split job. Parallel to JobExtensionMethods rather than shared,
/// because those select finalization activities that write ModuleJobs and carry deployment
/// vocabulary. Keeping them apart means a change made for a deployment reason cannot alter how a
/// manual job closes — which matters more here, since a manual job left open blocks every future
/// manual job on the module through the filtered unique index.
/// </summary>
public static class SplitMonolithExtensionMethods
{
    public static EventActivityBinder<SplitMonolithSaga, TFaulted> ThenSplitFaulted<TFaulted>(this
        EventActivityBinder<SplitMonolithSaga, TFaulted> binder, State failed, ILogger logger)
        where TFaulted : StepResponseBase
    {
        return binder
            .Then(x => logger.LogInformation(
                "SplitMonolith failed for Module {ModuleId}, job {JobId}", x.Saga.ModuleId, x.Saga.CorrelationId))
            .Publish(context => new SplitMonolithFailed
            {
                ModuleId = context.Saga.ModuleId,
                OrganizationId = context.Saga.OrganizationId,
                ModuleJobId = context.Saga.CorrelationId
            })
            .Activity(x => x.OfType<FailManualModuleJobActivity<SplitMonolithSaga, TFaulted>>())
            .TransitionTo(failed)
            .Finalize();
    }

    public static EventActivityBinder<SplitMonolithSaga, TCancelled> ThenSplitCancelled<TCancelled>(this
        EventActivityBinder<SplitMonolithSaga, TCancelled> binder, State cancelled)
        where TCancelled : StepResponseBase
    {
        return binder
            .Publish(context => new SplitMonolithCancelled
            {
                ModuleId = context.Saga.ModuleId,
                OrganizationId = context.Saga.OrganizationId,
                ModuleJobId = context.Saga.CorrelationId
            })
            .Activity(x => x.OfType<CancelManualModuleJobActivity<SplitMonolithSaga, TCancelled>>())
            .TransitionTo(cancelled)
            .Finalize();
    }

    public static EventActivityBinder<SplitMonolithSaga, TCompleted> ThenSplitCompleted<TCompleted>(this
        EventActivityBinder<SplitMonolithSaga, TCompleted> binder, State completed)
        where TCompleted : StepResponseBase
    {
        return binder
            .Publish(context => new SplitMonolithCompleted
            {
                ModuleId = context.Saga.ModuleId,
                OrganizationId = context.Saga.OrganizationId,
                ModuleJobId = context.Saga.CorrelationId
            })
            .Activity(x => x.OfType<CompleteManualModuleJobActivity<SplitMonolithSaga, TCompleted>>())
            .TransitionTo(completed)
            .Finalize();
    }

    /// <summary>
    /// A cancel clicked again after its timeout should already have fired. The timeout was lost, so
    /// nothing else will end this job: close it here rather than leaving the saga cancelling forever.
    /// </summary>
    public static EventActivityBinder<SplitMonolithSaga, CancelManualModuleJobRequested> ThenSplitCancelForced(this
        EventActivityBinder<SplitMonolithSaga, CancelManualModuleJobRequested> binder, ILogger logger, State cancelled)
    {
        return binder
            .Then(context => logger.LogWarning(
                "SplitMonolith: cancel re-requested for job {JobId} after its timeout was due; forcing it closed",
                context.Saga.CorrelationId))
            .Publish(context => new SplitMonolithCancelled
            {
                ModuleId = context.Saga.ModuleId,
                OrganizationId = context.Saga.OrganizationId,
                ModuleJobId = context.Saga.CorrelationId,
                CancellationReason = CancellationReason.UserRequested
            })
            .Activity(x => x.OfType<CancelManualModuleJobActivity<SplitMonolithSaga, CancelManualModuleJobRequested>>())
            .TransitionTo(cancelled)
            .Finalize();
    }

    /// <summary>
    /// A cancel request the runner never answers. Ends the job rather than leaving the saga in
    /// Cancelling: the commonest cause is that no step had been dispatched, so there was nothing
    /// on the runner to cancel.
    /// </summary>
    public static EventActivityBinder<SplitMonolithSaga, RequestTimeoutExpired<TRequest>> ThenSplitCancelTimedOut<TRequest>(this
        EventActivityBinder<SplitMonolithSaga, RequestTimeoutExpired<TRequest>> binder, ILogger logger, State cancelled)
        where TRequest : class
    {
        return binder
            .Then(context => logger.LogInformation(
                "SplitMonolith: cancel request timed out for job {JobId}; ending it anyway", context.Saga.CorrelationId))
            .Publish(context => new SplitMonolithCancelled
            {
                ModuleId = context.Saga.ModuleId,
                OrganizationId = context.Saga.OrganizationId,
                ModuleJobId = context.Saga.CorrelationId,
                CancellationReason = CancellationReason.UserRequested
            })
            .Activity(x => x.OfType<CancelManualModuleJobActivity<SplitMonolithSaga, RequestTimeoutExpired<TRequest>>>())
            .TransitionTo(cancelled)
            .Finalize();
    }

    public static EventActivityBinder<SplitMonolithSaga, HeartbeatFailed> ThenSplitTimedOut(this
        EventActivityBinder<SplitMonolithSaga, HeartbeatFailed> binder, State failed)
    {
        return binder
            .Publish(context => new SplitMonolithFailed
            {
                ModuleId = context.Saga.ModuleId,
                ModuleJobId = context.Saga.CorrelationId,
                OrganizationId = context.Saga.OrganizationId
            })
            .Activity(x => x.OfType<FailManualModuleJobActivity<SplitMonolithSaga, HeartbeatFailed>>())
            .TransitionTo(failed)
            .Finalize();
    }
}
