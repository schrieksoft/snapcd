// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using MassTransit.Contracts;
using Microsoft.Extensions.Logging;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.StateMachine.RippleProbes.Finalization;

namespace SnapCd.Server.Core.StateMachine.RippleProbes;

/// <summary>
/// Terminal transitions for a ripple probe. Parallel to JobExtensionMethods rather than shared,
/// because those select finalization activities that write ModuleJobs and carry deployment
/// vocabulary a probe has no equivalent for.
/// </summary>
public static class RippleProbeExtensionMethods
{
    public static EventActivityBinder<RippleProbeSaga, TFaulted> ThenRippleProbeFaulted<TFaulted>(this
        EventActivityBinder<RippleProbeSaga, TFaulted> binder, State failed, ILogger logger)
        where TFaulted : StepResponseBase
    {
        return binder
            .Then(x => logger.LogInformation(
                "Ripple probe failed for Module {ModuleId}, job {JobId}", x.Saga.ModuleId, x.Saga.CorrelationId))
            .Publish(context => new RippleProbeFailed
            {
                ModuleId = context.Saga.ModuleId,
                OrganizationId = context.Saga.OrganizationId,
                ModuleJobId = context.Saga.CorrelationId
            })
            .Activity(x => x.OfType<FailRippleProbeJobActivity<TFaulted>>())
            .TransitionTo(failed)
            .Finalize();
    }

    public static EventActivityBinder<RippleProbeSaga, TCancelled> ThenRippleProbeCancelled<TCancelled>(this
        EventActivityBinder<RippleProbeSaga, TCancelled> binder, State cancelled)
        where TCancelled : StepResponseBase
    {
        return binder
            .Publish(context => new RippleProbeCancelled
            {
                ModuleId = context.Saga.ModuleId,
                OrganizationId = context.Saga.OrganizationId,
                ModuleJobId = context.Saga.CorrelationId
            })
            .Activity(x => x.OfType<CancelRippleProbeJobActivity<TCancelled>>())
            .TransitionTo(cancelled)
            .Finalize();
    }

    public static EventActivityBinder<RippleProbeSaga, TCompleted> ThenRippleProbeCompleted<TCompleted>(this
        EventActivityBinder<RippleProbeSaga, TCompleted> binder, State completed)
        where TCompleted : StepResponseBase
    {
        return binder
            .Publish(context => new RippleProbeCompleted
            {
                ModuleId = context.Saga.ModuleId,
                OrganizationId = context.Saga.OrganizationId,
                ModuleJobId = context.Saga.CorrelationId
            })
            .Activity(x => x.OfType<CompleteRippleProbeJobActivity<TCompleted>>())
            .TransitionTo(completed)
            .Finalize();
    }

    /// <summary>
    /// A cancel clicked again after its timeout should already have fired. The timeout was lost, so
    /// nothing else will end this job: close it here rather than leaving the saga cancelling forever.
    /// </summary>
    public static EventActivityBinder<RippleProbeSaga, CancelRippleProbeJobRequested> ThenRippleProbeCancelForced(this
        EventActivityBinder<RippleProbeSaga, CancelRippleProbeJobRequested> binder, ILogger logger, State cancelled)
    {
        return binder
            .Then(context => logger.LogWarning(
                "Ripple probe: cancel re-requested for job {JobId} after its timeout was due; forcing it closed",
                context.Saga.CorrelationId))
            .Publish(context => new RippleProbeCancelled
            {
                ModuleId = context.Saga.ModuleId,
                OrganizationId = context.Saga.OrganizationId,
                ModuleJobId = context.Saga.CorrelationId,
                CancellationReason = CancellationReason.UserRequested
            })
            .Activity(x => x.OfType<CancelRippleProbeJobActivity<CancelRippleProbeJobRequested>>())
            .TransitionTo(cancelled)
            .Finalize();
    }

    /// <summary>
    /// A cancel request the runner never answers. Ends the job rather than leaving the saga in
    /// Cancelling: the commonest cause is that no step had been dispatched, so there was nothing
    /// on the runner to cancel.
    /// </summary>
    public static EventActivityBinder<RippleProbeSaga, RequestTimeoutExpired<TRequest>> ThenRippleProbeCancelTimedOut<TRequest>(this
        EventActivityBinder<RippleProbeSaga, RequestTimeoutExpired<TRequest>> binder, ILogger logger, State cancelled)
        where TRequest : class
    {
        return binder
            .Then(context => logger.LogInformation(
                "Ripple probe: cancel request timed out for job {JobId}; ending it anyway", context.Saga.CorrelationId))
            .Publish(context => new RippleProbeCancelled
            {
                ModuleId = context.Saga.ModuleId,
                OrganizationId = context.Saga.OrganizationId,
                ModuleJobId = context.Saga.CorrelationId,
                CancellationReason = CancellationReason.UserRequested
            })
            .Activity(x => x.OfType<CancelRippleProbeJobActivity<RequestTimeoutExpired<TRequest>>>())
            .TransitionTo(cancelled)
            .Finalize();
    }

    public static EventActivityBinder<RippleProbeSaga, HeartbeatFailed> ThenRippleProbeTimedOut(this
        EventActivityBinder<RippleProbeSaga, HeartbeatFailed> binder, State failed)
    {
        return binder
            .Publish(context => new RippleProbeFailed
            {
                ModuleId = context.Saga.ModuleId,
                ModuleJobId = context.Saga.CorrelationId,
                OrganizationId = context.Saga.OrganizationId
            })
            .Activity(x => x.OfType<FailRippleProbeJobActivity<HeartbeatFailed>>())
            .TransitionTo(failed)
            .Finalize();
    }
}
