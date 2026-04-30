using MassTransit;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.StateMachine.Gatekeeping;

public class ModuleModifiedStateMachine :
    MassTransitStateMachine<ModuleModifiedSaga>
{
    public required State WaitingForMoreEvents { get; set; }
    public required State Idle { get; set; }
    public required Event<ModuleModifiedTriggerRequested> ModuleModifiedTriggerRequested { get; set; }

    public required Schedule<ModuleModifiedSaga, ModuleModifiedWaitForNextTimeoutScheduled> ModuleModifiedWaitForNextTimeoutScheduled { get; set; }

    public ModuleModifiedStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => ModuleModifiedTriggerRequested, e => e.CorrelateById(context => context.Message.ModuleId));

        Schedule(() => ModuleModifiedWaitForNextTimeoutScheduled,
            saga => saga.TimeoutTokenId,
            config =>
            {
                config.Delay = TimeSpan.FromSeconds(5);
                config.Received = e => e.CorrelateById(context => context.Message.CorrelationId);
            });


        // We don't have an "Initially" section. We create the Saga directly in Db from the "Create<Module>(Module entity)" method in the ModuleRepository class.
        // Initially(...);

        During(Idle,
            When(ModuleModifiedTriggerRequested)
                .Then(_ => Console.WriteLine("Received ModuleModifiedTriggerRequested"))
                .Schedule(ModuleModifiedWaitForNextTimeoutScheduled,
                    context => new ModuleModifiedWaitForNextTimeoutScheduled
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        OrganizationId = context.Saga.OrganizationId
                    }
                )
                .TransitionTo(WaitingForMoreEvents)
                .Then(_ => Console.WriteLine("Transitioning to WaitingForMoreEvents"))
        );

        During(WaitingForMoreEvents,
            When(ModuleModifiedTriggerRequested)
                .Then(UpdateLastUpdated)
                .Unschedule(ModuleModifiedWaitForNextTimeoutScheduled)
                .Schedule(ModuleModifiedWaitForNextTimeoutScheduled,
                    context => new ModuleModifiedWaitForNextTimeoutScheduled
                    {
                        CorrelationId = context.Saga.CorrelationId,
                        OrganizationId = context.Saga.OrganizationId
                    }),
            When(ModuleModifiedWaitForNextTimeoutScheduled!.Received)
                .Then(c => Console.WriteLine($"Publishing GatekeepingJobRequested with ModuleId {c.Saga.CorrelationId}"))
                .Publish(context => new GatekeepingJobRequested
                {
                    ModuleId = context.Saga.CorrelationId,
                    OrganizationId = context.Saga.OrganizationId,
                    DesiredStateHeadline = DesiredStateHeadline.Applied
                }, publishContext => { publishContext.TimeToLive = TimeSpan.FromMinutes(5); })
                .Then(RemoveLastUpdated)
                .TransitionTo(Idle)
        );
    }


    private void RemoveLastUpdated<T>(BehaviorContext<ModuleModifiedSaga, T> context)
        where T : class
    {
        context.Saga.LastUpdated = null;
    }

    private void UpdateLastUpdated<T>(BehaviorContext<ModuleModifiedSaga, T> context)
        where T : class
    {
        context.Saga.LastUpdated = DateTime.UtcNow;
    }
}