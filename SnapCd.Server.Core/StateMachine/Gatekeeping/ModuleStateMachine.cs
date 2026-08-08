// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Gatekeeping;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.StateMachine.Gatekeeping.Activities;

namespace SnapCd.Server.Core.StateMachine.Gatekeeping;

public class ModuleStateMachine : MassTransitStateMachine<ModuleSaga>
{
    private readonly ILogger<ModuleStateMachine> _logger;

    public required State Gatekeeping { get; set; }

    // Events
    public required Event<GatekeepingJobRequested> GatekeepingJobRequested { get; set; }
    public required Event<RunQueueNowRequested> RunQueueNowRequested { get; set; }

    public required Event<ClearQueueRequested> ClearQueueRequested { get; set; }

    public required Event<ApplyModuleCompleted> ApplyModuleCompleted { get; set; }
    public required Event<ApplyModuleCancelled> ApplyModuleCancelled { get; set; }
    public required Event<ApplyModuleFailed> ApplyModuleFailed { get; set; }


    public required Event<DestroyModuleCompleted> DestroyModuleCompleted { get; set; }
    public required Event<DestroyModuleCancelled> DestroyModuleCancelled { get; set; }
    public required Event<DestroyModuleFailed> DestroyModuleFailed { get; set; }

    public required Event<ResourceCountRefreshedEvent> ResourceCountRefreshedEvent { get; set; }

    public required Event<ModuleDependencyCheckRequested> ModuleDependencyCheckRequested { get; set; }

    // Drift check schedule
    public Schedule<ModuleSaga, DriftCheckScheduled> DriftCheckScheduled { get; set; } = null!;


    public ModuleStateMachine(ILogger<ModuleStateMachine> logger)
    {
        _logger = logger;

        InstanceState(x => x.CurrentState);

        // Correlate events by ModuleId
        Event(() => GatekeepingJobRequested, e => e.CorrelateById(x => x.Message.ModuleId));

        Event(() => RunQueueNowRequested, e => e.CorrelateById(x => x.Message.ModuleId));
        Event(() => ClearQueueRequested, e => e.CorrelateById(x => x.Message.ModuleId));

        Event(() => ApplyModuleCompleted, e => e.CorrelateById(x => x.Message.ModuleId));
        Event(() => ApplyModuleCancelled, e => e.CorrelateById(x => x.Message.ModuleId));
        Event(() => ApplyModuleFailed, e => e.CorrelateById(x => x.Message.ModuleId));

        Event(() => DestroyModuleCompleted, e => e.CorrelateById(x => x.Message.ModuleId));
        Event(() => DestroyModuleCancelled, e => e.CorrelateById(x => x.Message.ModuleId));
        Event(() => DestroyModuleFailed, e => e.CorrelateById(x => x.Message.ModuleId));

        Event(() => ResourceCountRefreshedEvent, e => e.CorrelateById(x => x.Message.ModuleId));
        Event(() => ModuleDependencyCheckRequested, e => e.CorrelateById(x => x.Message.ModuleId));

        Schedule(() => DriftCheckScheduled, saga => saga.DriftCheckScheduleTokenId, config =>
        {
            config.Received = e => e.CorrelateById(context => context.Message.ModuleId);
        });


        // We don't have an "Initially" section. We create the Saga directly in Db from the "Create<Module>(Module entity)" method in the ModuleRepository class.
        // Initially(...);

        During(Gatekeeping,
            When(GatekeepingJobRequested)
                .Then(x => _logger.LogDebug(
                    "Received GatekeepingJobRequested with ID {ModuleId} in Gatekeeping state",
                    x.Message.ModuleId))
                .Unschedule(DriftCheckScheduled)
                .Then(x =>
                {
                    if (x.Message.DefinitiveRevision != null)
                        x.Saga.DesiredDefinitiveRevision = x.Message.DefinitiveRevision;
                    if (x.Message.DesiredClosureHash != null)
                        x.Saga.DesiredClosureHash = x.Message.DesiredClosureHash;
                })
                .Activity(x => x.OfType<TriggerModuleJobActivity<GatekeepingJobRequested>>())
                .Publish(x => new ModuleSagaModifiedEvent { ModuleId = x.Saga.CorrelationId, OrganizationId = x.Saga.OrganizationId }, context => { context.TimeToLive = TimeSpan.FromSeconds(120); })
                .Publish(x => new ModuleStateModifiedEvent { ModuleId = x.Saga.CorrelationId, OrganizationId = x.Saga.OrganizationId }, context => { context.TimeToLive = TimeSpan.FromSeconds(120); }),
            When(RunQueueNowRequested)
                .Activity(x => x.OfType<DequeueModuleJobActivity<ModuleSaga, RunQueueNowRequested>>())
                .Publish(x => new ModuleSagaModifiedEvent { ModuleId = x.Saga.CorrelationId, OrganizationId = x.Saga.OrganizationId }, context => { context.TimeToLive = TimeSpan.FromSeconds(120); })
                .Publish(x => new ModuleStateModifiedEvent { ModuleId = x.Saga.CorrelationId, OrganizationId = x.Saga.OrganizationId }, context => { context.TimeToLive = TimeSpan.FromSeconds(120); }),
            When(ClearQueueRequested)
                .Then(_ => _logger.LogDebug("Clearing queue"))
                .Then(y => { y.Saga.QueuedDesiredStateHeadline = null; })
                .Publish(x => new ModuleSagaModifiedEvent { ModuleId = x.Saga.CorrelationId, OrganizationId = x.Saga.OrganizationId }, context => { context.TimeToLive = TimeSpan.FromSeconds(120); })
                .Publish(x => new ModuleStateModifiedEvent { ModuleId = x.Saga.CorrelationId, OrganizationId = x.Saga.OrganizationId }, context => { context.TimeToLive = TimeSpan.FromSeconds(120); }),
            When(ApplyModuleCompleted)
                .Publish(x => new ModuleSagaModifiedEvent { ModuleId = x.Saga.CorrelationId, OrganizationId = x.Saga.OrganizationId }, context => { context.TimeToLive = TimeSpan.FromSeconds(120); })
                .Publish(x => new ModuleStateModifiedEvent { ModuleId = x.Saga.CorrelationId, OrganizationId = x.Saga.OrganizationId }, context => { context.TimeToLive = TimeSpan.FromSeconds(120); })
                .Activity(x => x.OfType<MaybeEmitModuleStateChangedToAppliedEvent<ApplyModuleCompleted>>())
                .Activity(x => x.OfType<DequeueIfDependenciesMetJobActivity<ApplyModuleCompleted>>())
                .Activity(x => x.OfType<ScheduleDriftCheckActivity<ApplyModuleCompleted>>()),
            When(ApplyModuleCancelled)
                .Publish(x => new ModuleSagaModifiedEvent { ModuleId = x.Saga.CorrelationId, OrganizationId = x.Saga.OrganizationId }, context => { context.TimeToLive = TimeSpan.FromSeconds(120); })
                .Publish(x => new ModuleStateModifiedEvent { ModuleId = x.Saga.CorrelationId, OrganizationId = x.Saga.OrganizationId }, context => { context.TimeToLive = TimeSpan.FromSeconds(120); })
                .Activity(x => x.OfType<DequeueIfDependenciesMetJobActivity<ApplyModuleCancelled>>())
                .Activity(x => x.OfType<ScheduleDriftCheckActivity<ApplyModuleCancelled>>()),
            When(ApplyModuleFailed)
                .Publish(x => new ModuleSagaModifiedEvent { ModuleId = x.Saga.CorrelationId, OrganizationId = x.Saga.OrganizationId }, context => { context.TimeToLive = TimeSpan.FromSeconds(120); })
                .Publish(x => new ModuleStateModifiedEvent { ModuleId = x.Saga.CorrelationId, OrganizationId = x.Saga.OrganizationId }, context => { context.TimeToLive = TimeSpan.FromSeconds(120); })
                .Activity(x => x.OfType<DequeueIfDependenciesMetJobActivity<ApplyModuleFailed>>())
                .Activity(x => x.OfType<ScheduleDriftCheckActivity<ApplyModuleFailed>>()),
            When(DestroyModuleCompleted)
                .Publish(x => new ModuleSagaModifiedEvent { ModuleId = x.Saga.CorrelationId, OrganizationId = x.Saga.OrganizationId }, context => { context.TimeToLive = TimeSpan.FromSeconds(120); })
                .Publish(x => new ModuleStateModifiedEvent { ModuleId = x.Saga.CorrelationId, OrganizationId = x.Saga.OrganizationId }, context => { context.TimeToLive = TimeSpan.FromSeconds(120); })
                .Activity(x => x.OfType<MaybeEmitModuleStateChangedToDestroyedEvent<DestroyModuleCompleted>>())
                .Activity(x => x.OfType<DequeueIfDependenciesMetJobActivity<DestroyModuleCompleted>>())
                .Unschedule(DriftCheckScheduled),
            When(DestroyModuleCancelled)
                .Publish(x => new ModuleSagaModifiedEvent { ModuleId = x.Saga.CorrelationId, OrganizationId = x.Saga.OrganizationId }, context => { context.TimeToLive = TimeSpan.FromSeconds(120); })
                .Publish(x => new ModuleStateModifiedEvent { ModuleId = x.Saga.CorrelationId, OrganizationId = x.Saga.OrganizationId }, context => { context.TimeToLive = TimeSpan.FromSeconds(120); })
                .Activity(x => x.OfType<DequeueIfDependenciesMetJobActivity<DestroyModuleCancelled>>())
                .Unschedule(DriftCheckScheduled),
            When(DestroyModuleFailed)
                .Publish(x => new ModuleSagaModifiedEvent { ModuleId = x.Saga.CorrelationId, OrganizationId = x.Saga.OrganizationId }, context => { context.TimeToLive = TimeSpan.FromSeconds(120); })
                .Publish(x => new ModuleStateModifiedEvent { ModuleId = x.Saga.CorrelationId, OrganizationId = x.Saga.OrganizationId }, context => { context.TimeToLive = TimeSpan.FromSeconds(120); })
                .Activity(x => x.OfType<DequeueIfDependenciesMetJobActivity<DestroyModuleFailed>>())
                .Unschedule(DriftCheckScheduled),
            When(ResourceCountRefreshedEvent)
                .Then(y =>
                {
                    var oldCount = y.Saga.ActualResourceCount;
                    y.Saga.ActualResourceCount = y.Message.ActualResourceCount;

                    // Only publish event if the count actually changed
                    if (oldCount != y.Message.ActualResourceCount)
                        y.Publish(new ModuleResourceCountUpdatedEvent
                        {
                            ModuleId = y.Saga.CorrelationId,
                            OrganizationId = y.Saga.OrganizationId
                        });
                })
                .Publish(x => new ModuleSagaModifiedEvent { ModuleId = x.Saga.CorrelationId, OrganizationId = x.Saga.OrganizationId }, context => { context.TimeToLive = TimeSpan.FromSeconds(120); })
                .Publish(x => new ModuleStateModifiedEvent { ModuleId = x.Saga.CorrelationId, OrganizationId = x.Saga.OrganizationId }, context => { context.TimeToLive = TimeSpan.FromSeconds(120); }),
            When(ModuleDependencyCheckRequested)
                .Then(y => _logger.LogDebug(
                    "Checking dependencies for queued module {ModuleId}", y.Message.ModuleId))
                .Activity(y => y.OfType<DequeueIfDependenciesMetJobActivity<ModuleDependencyCheckRequested>>())
                .Publish(x => new ModuleSagaModifiedEvent { ModuleId = x.Saga.CorrelationId, OrganizationId = x.Saga.OrganizationId }, context => { context.TimeToLive = TimeSpan.FromSeconds(120); })
                .Publish(x => new ModuleStateModifiedEvent { ModuleId = x.Saga.CorrelationId, OrganizationId = x.Saga.OrganizationId }, context => { context.TimeToLive = TimeSpan.FromSeconds(120); }),
            When(DriftCheckScheduled.Received)
                .IfElse(IsFromSupersededSchedule,
                    stale => stale,
                    current => current
                        .Then(x => _logger.LogDebug(
                            "Drift check fired for module {ModuleId}", x.Saga.CorrelationId))
                        .Publish(x => new GatekeepingJobRequested
                        {
                            ModuleId = x.Saga.CorrelationId,
                            OrganizationId = x.Saga.OrganizationId,
                            DesiredStateHeadline = DesiredStateHeadline.Applied,
                            SetNewDesiredState = false
                        }))
        );
    }

    // A tick whose scheduling token no longer matches the saga's was armed by a superseded
    // schedule; acting on it would run a duplicate drift check. A tokenless tick is accepted.
    private static bool IsFromSupersededSchedule(BehaviorContext<ModuleSaga, DriftCheckScheduled> context)
    {
        var messageTokenId = context.Headers.Get<Guid>(MessageHeaders.SchedulingTokenId);
        return messageTokenId.HasValue
               && context.Saga.DriftCheckScheduleTokenId.HasValue
               && messageTokenId.Value != context.Saga.DriftCheckScheduleTokenId.Value;
    }
}
