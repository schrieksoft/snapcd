// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Transfers;
using SnapCd.Server.Core.Services.Crud.Transfers;

namespace SnapCd.Server.Core.StateMachine.Transfers;

/// <summary>
/// One per Transfer, from the moment it is opened until it lands or is abandoned. It runs a prove
/// round whenever either branch moves, then the migration once both Modules have locked and merged,
/// so proving and migrating are two phases of one life rather than two unrelated jobs.
///
/// It dispatches nothing to a runner. Each Module's own saga does that and owns its retries; this
/// one decides what happens next and carries what has to cross between them - the state fragment,
/// and the values one Module needs from the other.
/// </summary>
public partial class TransferStateMachine : MassTransitStateMachine<TransferSaga>
{
    private readonly ILogger<TransferStateMachine> _logger;

    public Event<TransferOpened> Opened { get; } = null!;
    public Event<TransferProveRoundRequested> ProveRoundRequested { get; } = null!;

    // From the two Module sagas: one report each, whatever they did.
    public Event<TransferParticipantRan> Ran { get; } = null!;
    public Event<TransferParticipantStopped> ParticipantStopped { get; } = null!;

    /// <summary>Open, with no round running. Every round starts and ends here.</summary>
    public State Idle { get; } = null!;

    /// <summary>The source is running: checking out, planning, pinning its state, proving.</summary>
    public State SourceRunning { get; } = null!;

    /// <summary>The receiver is running, with the source's fragment in hand.</summary>
    public State ReceiverRunning { get; } = null!;

    /// <summary>
    /// The source is proving, having stopped after its state pull because it needed a value the
    /// receiver has now produced.
    /// </summary>
    public State SourceFinishing { get; } = null!;

    /// <summary>
    /// The round stopped short. Nothing is held by a prove, so this is a report rather than a
    /// predicament: the next push or the next launch runs the next round.
    /// </summary>
    public State Stalled { get; } = null!;

    public TransferStateMachine(ILogger<TransferStateMachine> logger)
    {
        _logger = logger;

        InstanceState(x => x.CurrentState);

        Event(() => Opened, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => ProveRoundRequested, x => x.CorrelateById(y => y.Message.CorrelationId));

        // Replies name the Transfer rather than the coordinator, because the Module sagas know
        // which Transfer they belong to, not which saga id coordinates it.
        Event(() => Ran, x => x.CorrelateBy((saga, context) => saga.TransferId == context.Message.TransferId));
        Event(() => ParticipantStopped, x => x.CorrelateBy((saga, context) => saga.TransferId == context.Message.TransferId));

        Initially(
            When(Opened)
                .Then(context =>
                {
                    context.Saga.CorrelationId = context.Message.CorrelationId;
                    context.Saga.OrganizationId = context.Message.OrganizationId;
                    context.Saga.TransferId = context.Message.TransferId;
                    context.Saga.SourceModuleId = context.Message.SourceModuleId;
                    context.Saga.ReceiverModuleId = context.Message.ReceiverModuleId;
                    context.Saga.MapHash = context.Message.MapHash;
                    context.Saga.SourceSagaId = Guid.NewGuid();
                    context.Saga.ReceiverSagaId = Guid.NewGuid();

                    _logger.LogInformation(
                        "Transfer {TransferId} opened: {SourceModuleId} into {ReceiverModuleId}",
                        context.Message.TransferId, context.Message.SourceModuleId, context.Message.ReceiverModuleId);
                })
                .Then(context =>
                {
                    context.Publish(new TransferParticipantRegistered
                    {
                        CorrelationId = context.Saga.SourceSagaId,
                        OrganizationId = context.Saga.OrganizationId,
                        TransferId = context.Saga.TransferId,
                        Role = TransferRole.Source,
                        Declared = context.Message.SourceDeclared,
                        RootDirectory = context.Message.SourceRootDirectory
                    });

                    context.Publish(new TransferParticipantRegistered
                    {
                        CorrelationId = context.Saga.ReceiverSagaId,
                        OrganizationId = context.Saga.OrganizationId,
                        TransferId = context.Saga.TransferId,
                        Role = TransferRole.Receiver,
                        Declared = context.Message.ReceiverDeclared,
                        RootDirectory = context.Message.ReceiverRootDirectory
                    });
                })
                .TransitionTo(Idle)
        );

        Configure_ProveRound();
    }

    /// <summary>Records why the round stopped and tells both Modules to stop where they are.</summary>
    private async Task Stall<TMessage>(BehaviorContext<TransferSaga, TMessage> context, string reason)
        where TMessage : class
    {
        context.Saga.StallReason = reason;

        _logger.LogInformation(
            "Transfer {TransferId}: round {Round} stopped - {Reason}",
            context.Saga.TransferId, context.Saga.ProveRound, reason);

        var (source, receiver) = SagaIds(context.Saga);
        foreach (var participant in new[] { source, receiver })
            await context.Publish(new TransferParticipantStopRequested
            {
                CorrelationId = participant,
                OrganizationId = context.Saga.OrganizationId,
                Immediate = false
            });
    }

    private Task StopRound(BehaviorContext<TransferSaga, TransferParticipantStopped> context) =>
        Stall(context,
            $"Module {context.Message.ModuleId} stopped at {context.Message.Task}: " +
            (context.Message.ErrorHeader ?? context.Message.Status.ToString()));

    /// <summary>
    /// Keeps whatever a run produced for the other Module: the state fragment, and the values its
    /// plan produced. Both are encrypted, because a fragment is raw state.
    /// </summary>
    private static async Task StoreFromRun(BehaviorContext<TransferSaga, TransferParticipantRan> context)
    {
        if (context.Saga.CurrentJobId is not { } jobId) return;

        var artefacts = PipeExtensions.GetPayload<IServiceProvider>(context)
            .GetRequiredService<TransferArtefactService>();

        if (context.Message.FragmentState != null)
            await artefacts.Store(jobId, context.Saga.OrganizationId,
                "fragment.tfstate", context.Message.FragmentState);

        if (context.Message.FragmentMeta != null)
            await artefacts.Store(jobId, context.Saga.OrganizationId,
                "fragment.yaml", context.Message.FragmentMeta);

        foreach (var (name, value) in context.Message.Outputs)
            await artefacts.Store(jobId, context.Saga.OrganizationId, name, value);
    }

    /// <summary>
    /// Ends a round. Nothing a round produced outlives it: the fragment and the passed values are
    /// deleted, and the verdict is read back off the step rows.
    /// </summary>
    private static async Task FinishRound<TMessage>(BehaviorContext<TransferSaga, TMessage> context)
        where TMessage : class
    {
        if (context.Saga.CurrentJobId is not { } jobId) return;

        var artefacts = PipeExtensions.GetPayload<IServiceProvider>(context)
            .GetRequiredService<TransferArtefactService>();

        await artefacts.DeleteForJob(jobId, context.Saga.OrganizationId);

        context.Saga.CurrentJobId = null;
        context.Saga.Map = null;
    }

    /// <summary>The Module sagas' correlation ids, in the order the two Modules are named.</summary>
    private static (Guid Source, Guid Receiver) SagaIds(TransferSaga saga) =>
        (saga.SourceSagaId, saga.ReceiverSagaId);

    private static ManualJobStepService Steps<TMessage>(BehaviorContext<TransferSaga, TMessage> context)
        where TMessage : class =>
        PipeExtensions.GetPayload<IServiceProvider>(context).GetRequiredService<ManualJobStepService>();
}
