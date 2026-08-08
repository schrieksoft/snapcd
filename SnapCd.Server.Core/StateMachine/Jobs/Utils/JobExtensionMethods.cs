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

    public static EventActivityBinder<TSaga, Fault<TRequestMessage>> ThenFaulted<TSaga, TResponseFailed, TRequestMessage>(this
        EventActivityBinder<TSaga, Fault<TRequestMessage>> binder, State failed, ILogger logger)
        where TSaga : JobSagaBase
        where TRequestMessage : StepRequestBase, new()
        where TResponseFailed : ModuleJobEventCompletedBase, new()
    {
        return binder
            .Then(x => logger.LogInformation($"Plan failed for Module {x.Saga.ModuleId} and ModuleJobId {x.Saga.CorrelationId}"))
            .Publish(context => new TResponseFailed
            {
                ModuleId = context.Saga.ModuleId,
                OrganizationId = context.Saga.OrganizationId,
                ModuleJobId = context.Saga.CorrelationId
            })
            .Activity(x => x.OfType<FailModuleJobRequestActivity<TSaga, TRequestMessage>>())
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
            .Then(x => logger.LogInformation($"Plan failed for Module {x.Saga.ModuleId} and ModuleJobId {x.Saga.CorrelationId}"))
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
            .TransitionTo(cancelled)
            .Finalize();
    }


    public static EventActivityBinder<TSaga, HeartbeatScheduled> ThenHeartbeatScheduled<TSaga>(this
        EventActivityBinder<TSaga, HeartbeatScheduled> binder, Request<TSaga, HeartbeatRequested, HeartbeatCompleted> heartbeatRequested)
        where TSaga : JobSagaBase
    {
        return binder
            .IfElse(IsFromSupersededSchedule,
                stale => stale,
                current => current
                    .Request(heartbeatRequested,
                        context => new HeartbeatRequested
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
            .Then(_ => { Console.WriteLine("Heartbeat received, scheduling new one."); })
            .Schedule(heartbeatScheduled,
                context => new HeartbeatScheduled { CorrelationId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId });
    }

    public static EventActivityBinder<TSaga, HeartbeatFailed> ThenJobTimedOut<TSaga, TResponseFailed>(this
        EventActivityBinder<TSaga, HeartbeatFailed> binder, State failed)
        where TSaga : JobSagaBase
        where TResponseFailed : ModuleJobEventCompletedBase, new()
    {
        return binder
            .Then(_ => { Console.WriteLine("Heartbeat failed. Finalizing."); })
            .Publish(context => new TResponseFailed
            {
                ModuleId = context.Saga.ModuleId,
                ModuleJobId = context.Saga.CorrelationId,
                OrganizationId = context.Saga.OrganizationId
            })
            .Activity(x => x.OfType<TimeoutModuleJobActivity<TSaga, HeartbeatRequested>>())
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
            .Publish(context =>
            {
                cancelled.ModuleId = context.Saga.ModuleId;
                cancelled.ModuleJobId = context.Saga.CorrelationId;
                cancelled.OrganizationId = context.Saga.OrganizationId;
                cancelled.CancellationReason ??= CancellationReason.UserRequested;
                return cancelled;
            })
            .Activity(x => x.OfType<CancelModuleJobActivity<TSaga, TEvent>>())
            .TransitionTo(transitionTo)
            .Finalize();
    }


    public static EventActivityBinder<TSaga, CancelModuleRequested> IfCancelKill<TSaga, TResponseCancelled>(
        this EventActivityBinder<TSaga, CancelModuleRequested> binder,
        ILogger logger,
        Request<TSaga, CancelKillRequested, DummyCancelKillCompleted> killCancelRequested,
        State? transitionTo,
        State? cancelledState
    )
        where TSaga : JobSagaBase
        where TResponseCancelled : ModuleJobEventCompletedBase, new()
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
                                var endpointUri = MassTransitHelpers.GetConsumerEndpoint(
                                    context.Saga.ServerInstanceId.Value,
                                    nameof(CancelKillRequested));
                                return new Uri(endpointUri);
                            }

                            // MassTransit's address-provider lambda allows null to mean "use default address",
                            // even though the declared return type is non-nullable Uri.
#pragma warning disable CS8603
                            return null;
#pragma warning restore CS8603
                        },
                        context => new CancelKillRequested
                        {
                            OrganizationId = context.Saga.OrganizationId,
                            CorrelationId = context.Saga.CorrelationId,
                            RunnerInstanceName = context.Saga.RunnerInstanceName,
                            RunnerId = context.Saga.RunnerId
                        })
            );
    }


    public static EventActivityBinder<TSaga, CancelModuleRequested> IfCancelGraceful<TSaga, TResponseCancelled>(
        this EventActivityBinder<TSaga, CancelModuleRequested> binder,
        ILogger logger,
        Request<TSaga, CancelGracefulRequested, DummyCancelGracefulCompleted> gracefulCancelRequested,
        State? transitionTo,
        State? cancelledState
    )
        where TSaga : JobSagaBase
        where TResponseCancelled : ModuleJobEventCompletedBase, new()
    {
        return binder
            .If(x => x.Message.CancellationType == CancellationType.ImmediateGraceful,
                x => x
                    .Then(_ => { logger.LogDebug("Publishing GracefulCancelRequested and transitioning to Cancelling"); })
                    .Then(context =>
                    {
                        context.Saga.PreviousStateBeforeCancelling = context.Saga.CurrentState;
                        context.Saga.WaitingSince = DateTime.UtcNow;
                    })
                    .TransitionTo(transitionTo)
                    .Request(gracefulCancelRequested,
                        context =>
                        {
                            if (context.Saga.ServerInstanceId.HasValue)
                            {
                                var endpointUri = MassTransitHelpers.GetConsumerEndpoint(
                                    context.Saga.ServerInstanceId.Value,
                                    nameof(CancelGracefulRequested));
                                return new Uri(endpointUri);
                            }

                            // MassTransit's address-provider lambda allows null to mean "use default address",
                            // even though the declared return type is non-nullable Uri.
#pragma warning disable CS8603
                            return null;
#pragma warning restore CS8603
                        },
                        context => new CancelGracefulRequested
                        {
                            OrganizationId = context.Saga.OrganizationId,
                            CorrelationId = context.Saga.CorrelationId,
                            RunnerInstanceName = context.Saga.RunnerInstanceName,
                            RunnerId = context.Saga.RunnerId
                        })
            );
    }


    public static EventActivityBinder<TSaga, CancelModuleRequested> IfCancelAfterCurrent<TSaga>(
        this EventActivityBinder<TSaga, CancelModuleRequested> binder,
        ILogger logger,
        State? transitionTo
    )
        where TSaga : JobSagaBase
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