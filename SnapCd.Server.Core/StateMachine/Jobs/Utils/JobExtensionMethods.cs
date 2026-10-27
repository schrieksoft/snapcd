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
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Events.Jobs.Base;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.StateMachine.Jobs.Activites.Finalization;

namespace SnapCd.Server.Core.StateMachine.Jobs.Utils;

public static class JobExtensionMethods
{
    /// <summary>
    /// The cancel request this saga is waiting on should have timed out by now. A margin over the
    /// request's own timeout keeps a merely-late timeout from being treated as a lost one.
    /// </summary>
    public static bool CancelTimeoutIsOverdue<TSaga, TCancelRequest>(BehaviorContext<TSaga, TCancelRequest> context)
        where TSaga : JobSagaBase
        where TCancelRequest : class, ICancelRequest
        => context.Saga.WaitingSince is { } since && DateTime.UtcNow - since > CancelRequestTimeout + TimeSpan.FromSeconds(15);

    /// <summary>The window a cancel request is given before its timeout is considered lost.</summary>
    public static readonly TimeSpan CancelRequestTimeout = TimeSpan.FromSeconds(90);

    /// <summary>
    /// A cancel clicked again after its timeout should already have fired. The timeout was lost, so
    /// nothing else will end this job: close it here rather than leaving the saga cancelling forever.
    /// Re-issuing instead would restart the clock, and repeated clicks would push the deadline out.
    /// </summary>
    public static EventActivityBinder<TSaga, TCancelRequest> ThenCancelForced<TSaga, TResponseCancelled, TCancelRequest>(this
        EventActivityBinder<TSaga, TCancelRequest> binder, ILogger logger, State cancelled)
        where TSaga : JobSagaBase
        where TResponseCancelled : ModuleJobEventCompletedBase, new()
        where TCancelRequest : class, ICancelRequest
    {
        return binder
            .Then(context => logger.LogWarning(
                "Cancel re-requested for job {JobId} after its timeout was due; forcing it closed",
                context.Saga.CorrelationId))
            .Publish(context => new TResponseCancelled
            {
                ModuleId = context.Saga.ModuleId,
                OrganizationId = context.Saga.OrganizationId,
                ModuleJobId = context.Saga.CorrelationId,
                CancellationReason = CancellationReason.UserRequested
            })
            .Activity(x => x.OfType<CancelModuleJobActivity<TSaga, TCancelRequest>>())
            .TransitionTo(cancelled)
            .Finalize();
    }

    public static EventActivityBinder<TSaga, RequestTimeoutExpired<TRequestMessage>> ThenCancelTimeout<TSaga, TResponseCancelled, TRequestMessage>(this
        EventActivityBinder<TSaga, RequestTimeoutExpired<TRequestMessage>> binder, ILogger logger, State cancelled)
        where TSaga : JobSagaBase
        where TRequestMessage : CorrelationBase, new()
        where TResponseCancelled : ModuleJobEventCompletedBase, new()
    {
        return binder
            .Then(_ => { logger.LogInformation("Cancel request timed out"); })
            .Publish(context => new TResponseCancelled
            {
                ModuleId = context.Saga.ModuleId,
                OrganizationId = context.Saga.OrganizationId,
                ModuleJobId = context.Saga.CorrelationId,
                CancellationReason = CancellationReason.UserRequested
            })
            .Activity(x => x.OfType<CancelOnTimeoutModuleJobActivity<TSaga, TRequestMessage>>())
            .TransitionTo(cancelled)
            .Finalize();
    }

    public static EventActivityBinder<TSaga, Fault<TMessage>> ThenFaultedOnFault<TSaga, TResponseFailed, TMessage>(this
        EventActivityBinder<TSaga, Fault<TMessage>> binder, State failed, ILogger logger)
        where TSaga : JobSagaBase
        where TMessage : class
        where TResponseFailed : ModuleJobEventCompletedBase, new()
    {
        return binder
            .Then(x => logger.LogInformation("Job {ModuleJobId} for Module {ModuleId} failed", x.Saga.CorrelationId, x.Saga.ModuleId))
            .Publish(context => new TResponseFailed
            {
                ModuleId = context.Saga.ModuleId,
                OrganizationId = context.Saga.OrganizationId,
                ModuleJobId = context.Saga.CorrelationId
            })
            .Activity(x => x.OfType<FailModuleJobOnFaultActivity<TSaga, TMessage>>())
            .TransitionTo(failed)
            .Finalize();
    }

    public static EventActivityBinder<TSaga, TFaultedMessage> ThenFaulted<TSaga, TResponseFailed, TFaultedMessage>(this
        EventActivityBinder<TSaga, TFaultedMessage> binder, State failed, ILogger logger)
        where TSaga : JobSagaBase
        where TFaultedMessage : StepResponseBase
        where TResponseFailed : ModuleJobEventCompletedBase, new()
    {
        return binder
            .Then(x => logger.LogInformation("Job {ModuleJobId} for Module {ModuleId} failed", x.Saga.CorrelationId, x.Saga.ModuleId))
            .Publish(context => new TResponseFailed
            {
                ModuleId = context.Saga.ModuleId,
                OrganizationId = context.Saga.OrganizationId,
                ModuleJobId = context.Saga.CorrelationId
            })
            .Activity(x => x.OfType<FailModuleJobActivity<TSaga, TFaultedMessage>>())
            .TransitionTo(failed)
            .Finalize();
    }

    public static EventActivityBinder<TSaga, TCancelledMessage> ThenCancelled<TSaga, TResponseCancelled, TCancelledMessage>(this
        EventActivityBinder<TSaga, TCancelledMessage> binder, State cancelled)
        where TSaga : JobSagaBase
        where TCancelledMessage : StepResponseBase
        where TResponseCancelled : ModuleJobEventCompletedBase, new()
    {
        return binder
            .Publish(context => new TResponseCancelled
            {
                ModuleId = context.Saga.ModuleId,
                OrganizationId = context.Saga.OrganizationId,
                ModuleJobId = context.Saga.CorrelationId,
                CancellationReason = CancellationReason.UserRequested
            })
            .Activity(x => x.OfType<CancelModuleJobActivity<TSaga, TCancelledMessage>>())
            .TransitionTo(cancelled)
            .Finalize();
    }


    /// <summary>
    /// TRequest names the job family, so the consumer that answers reads the right table. The
    /// type argument is what carries it: there is no family left for a caller to get wrong.
    /// </summary>
    public static EventActivityBinder<TSaga, HeartbeatScheduled> ThenHeartbeatScheduled<TSaga, TRequest>(this
        EventActivityBinder<TSaga, HeartbeatScheduled> binder, Request<TSaga, TRequest, HeartbeatCompleted> heartbeatRequested)
        where TSaga : JobSagaBase
        where TRequest : HeartbeatRequestedBase, new()
    {
        return binder
            .IfElse(IsFromSupersededSchedule,
                stale => stale,
                current => current
                    .Request(heartbeatRequested,
                        context => new TRequest
                        {
                            CorrelationId = context.Saga.CorrelationId,
                            OrganizationId = context.Saga.OrganizationId,
                            RunnerInstanceName = context.Saga.RunnerInstanceName,
                            RunnerId = context.Saga.RunnerId
                        }));
    }

    // A tick whose scheduling token no longer matches the saga's was armed by a superseded
    // cycle; acting on it would fork a second heartbeat loop. A tick without a token (the
    // reconciler's) is always accepted.
    private static bool IsFromSupersededSchedule<TSaga>(BehaviorContext<TSaga, HeartbeatScheduled> context)
        where TSaga : JobSagaBase
    {
        var messageTokenId = context.Headers.Get<Guid>(MessageHeaders.SchedulingTokenId);
        return messageTokenId.HasValue
               && context.Saga.HeartbeatScheduleTokenId.HasValue
               && messageTokenId.Value != context.Saga.HeartbeatScheduleTokenId.Value;
    }


    public static EventActivityBinder<TSaga, HeartbeatCompleted> ThenHeartbeatCompleted<TSaga>(this
        EventActivityBinder<TSaga, HeartbeatCompleted> binder, Schedule<TSaga, HeartbeatScheduled> heartbeatScheduled)
        where TSaga : JobSagaBase
    {
        return binder
            .Schedule(heartbeatScheduled,
                context => new HeartbeatScheduled { CorrelationId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId });
    }

    public static EventActivityBinder<TSaga, HeartbeatFailed> ThenJobTimedOut<TSaga, TResponseFailed>(this
        EventActivityBinder<TSaga, HeartbeatFailed> binder, State failed)
        where TSaga : JobSagaBase
        where TResponseFailed : ModuleJobEventCompletedBase, new()
    {
        return binder
            .Publish(context => new TResponseFailed
            {
                ModuleId = context.Saga.ModuleId,
                ModuleJobId = context.Saga.CorrelationId,
                OrganizationId = context.Saga.OrganizationId
            })
            .Activity(x => x.OfType<TimeoutModuleJobActivity<TSaga, ModuleJobHeartbeatRequested>>())
            .TransitionTo(failed)
            .Finalize();
    }


    public static EventActivityBinder<TSaga, TEvent> Cancel<TSaga, TEvent, TResponseCancelled>(this
        EventActivityBinder<TSaga, TEvent> binder, ILogger logger, State? transitionTo, TResponseCancelled cancelled)
        where TEvent : class
        where TSaga : JobSagaBase
        where TResponseCancelled : ModuleJobEventCompletedBase, new()
    {
        return binder
            .Then(_ =>
            {
                // Log the event type dynamically
                logger.LogDebug($"Cancelled via: {typeof(TEvent).Name}");
            })
            .Publish(context => new TResponseCancelled
            {
                ModuleId = context.Saga.ModuleId,
                ModuleJobId = context.Saga.CorrelationId,
                OrganizationId = context.Saga.OrganizationId,
                CancellationReason = cancelled.CancellationReason ?? CancellationReason.UserRequested
            })
            .Activity(x => x.OfType<CancelModuleJobActivity<TSaga, TEvent>>())
            .TransitionTo(transitionTo)
            .Finalize();
    }

    /// <summary>
    /// The runner is lost while the job waits to cancel after its current step: the cancel completes, and the lost runner is kept on the job.
    /// </summary>
    public static EventActivityBinder<TSaga, HeartbeatFailed> CancelAfterLostRunner<TSaga, TResponseCancelled>(this
        EventActivityBinder<TSaga, HeartbeatFailed> binder, ILogger logger, State transitionTo)
        where TSaga : JobSagaBase
        where TResponseCancelled : ModuleJobEventCompletedBase, new()
    {
        return binder
            .Activity(x => x.OfType<RecordLostRunnerActivity<TSaga>>())
            .Cancel(logger, transitionTo, new TResponseCancelled());
    }

    /// <summary>
    /// A step that faults while its job is being cancelled: the cancel completes, and a server-side fault is kept on the job.
    /// </summary>
    public static EventActivityBinder<TSaga, TFaulted> CancelAfterFault<TSaga, TFaulted, TResponseCancelled>(this
        EventActivityBinder<TSaga, TFaulted> binder, ILogger logger, State transitionTo)
        where TFaulted : StepFaultedBase
        where TSaga : JobSagaBase
        where TResponseCancelled : ModuleJobEventCompletedBase, new()
    {
        return binder
            .Activity(x => x.OfType<RecordServerSideFaultActivity<TSaga, TFaulted>>())
            .Cancel(logger, transitionTo, new TResponseCancelled());
    }


    public static EventActivityBinder<TSaga, TCancelRequest> IfCancelKill<TSaga, TResponseCancelled, TCancelRequest,
        TCancelKillRequested, TDummyCancelKillCompleted>(
        this EventActivityBinder<TSaga, TCancelRequest> binder,
        ILogger logger,
        Request<TSaga, TCancelKillRequested, TDummyCancelKillCompleted> killCancelRequested,
        State? transitionTo,
        State? cancelledState
    )
        where TSaga : JobSagaBase
        where TResponseCancelled : ModuleJobEventCompletedBase, new()
        where TCancelRequest : class, ICancelRequest
        where TCancelKillRequested : CancelKillRequestedBase, new()
        where TDummyCancelKillCompleted : class
    {
        return binder
            .If(x => x.Message.CancellationType == CancellationType.ImmediateKill,
                x => x
                    .Then(_ => { logger.LogDebug("Publishing KillCancelRequested and transitioning to Cancelling"); })
                    .Then(context =>
                    {
                        context.Saga.PreviousStateBeforeCancelling = context.Saga.CurrentState;
                        context.Saga.WaitingSince = DateTime.UtcNow;
                    })
                    .TransitionTo(transitionTo)
                    .Request(killCancelRequested,
                        context =>
                        {
                            if (context.Saga.ServerInstanceId.HasValue)
                            {
                                // The consumer's queue is named after the message type, which is
                                // per family; the property name is the same for all of them.
                                var endpointUri = MassTransitHelpers.GetConsumerEndpoint(
                                    context.Saga.ServerInstanceId.Value,
                                    typeof(TCancelKillRequested).Name);
                                return new Uri(endpointUri);
                            }

                            // MassTransit's address-provider lambda allows null to mean "use default address",
                            // even though the declared return type is non-nullable Uri.
#pragma warning disable CS8603
                            return null;
#pragma warning restore CS8603
                        },
                        context => new TCancelKillRequested
                        {
                            OrganizationId = context.Saga.OrganizationId,
                            CorrelationId = context.Saga.CorrelationId,
                            RunnerInstanceName = context.Saga.RunnerInstanceName,
                            RunnerId = context.Saga.RunnerId
                        })
            );
    }


    public static EventActivityBinder<TSaga, TCancelRequest> IfCancelAfterCurrent<TSaga, TCancelRequest>(
        this EventActivityBinder<TSaga, TCancelRequest> binder,
        ILogger logger,
        State? transitionTo
    )
        where TSaga : JobSagaBase
        where TCancelRequest : class, ICancelRequest
    {
        return binder
            .If(x => x.Message.CancellationType == CancellationType.AfterCurrent,
                x => x
                    .Then(_ => { logger.LogDebug("Transitioning to CancellingAfterCurrent"); })
                    .Then(context =>
                    {
                        context.Saga.PreviousStateBeforeCancelling = context.Saga.CurrentState;
                        context.Saga.WaitingSince = DateTime.UtcNow;
                    })
                    .TransitionTo(transitionTo)
            );
    }
}