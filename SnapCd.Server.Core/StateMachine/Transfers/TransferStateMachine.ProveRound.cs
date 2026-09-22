// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Transfers;

namespace SnapCd.Server.Core.StateMachine.Transfers;

public partial class TransferStateMachine
{
    /// <summary>
    /// A prove round. The coordinator makes three decisions and no more: who starts, who gets what
    /// the first one produced, and whether the round is green. Each Module runs its own sequence
    /// once told to start, and is not spoken to again until it reports.
    ///
    /// The source starts, because its state pull writes the fragment the receiver needs. It stops
    /// there when the receiver has to prove first - when the source needs a value the receiver
    /// produces - and is asked to finish once the receiver has produced it.
    /// </summary>
    private void Configure_ProveRound()
    {
        During(Idle,
            When(ProveRoundRequested)
                .Then(context =>
                {
                    context.Saga.CurrentJobId = context.Message.JobId;
                    context.Saga.ProveRound += 1;
                    context.Saga.Map = context.Message.Map;
                    context.Saga.SourceProveRef = context.Message.SourceProveRef;
                    context.Saga.ReceiverProveRef = context.Message.ReceiverProveRef;
                    context.Saga.StallReason = null;
                    context.Saga.SourceRan = false;
                    context.Saga.ReceiverRan = false;

                    _logger.LogInformation(
                        "Transfer {TransferId}: prove round {Round} starting",
                        context.Saga.TransferId, context.Saga.ProveRound);
                })
                .TransitionTo(SourceRunning)
                // The source goes first either way: only its run produces the fragment.
                .Publish(context => Run(context.Saga, TransferRole.Source, stopAfterMap: false))
        );

        During(SourceRunning,
            When(Ran)
                .Then(context =>
                {
                    context.Saga.SourceRan = true;
                    context.Saga.SourceNeedsValues = context.Message.NeedsValuesFrom.Count > 0;
                })
                .ThenAsync(StoreFromRun)
                .IfElse(
                    context => Refused(context.Message),
                    refused => refused.ThenAsync(StallOnRefusal).TransitionTo(Stalled),
                    ok => ok
                        .TransitionTo(ReceiverRunning)
                        .ThenAsync(context => RunReceiver(context))),

            When(ParticipantStopped).ThenAsync(StopRound).TransitionTo(Stalled)
        );

        During(ReceiverRunning,
            When(Ran)
                .Then(context => context.Saga.ReceiverRan = true)
                .ThenAsync(StoreFromRun)
                .IfElse(
                    context => Refused(context.Message),
                    refused => refused.ThenAsync(StallOnRefusal).TransitionTo(Stalled),
                    ok => ok.IfElse(
                        // The source stopped after its map because it needed a value the receiver
                        // has now produced; it goes back to finish.
                        context => context.Saga.SourceNeedsValues,
                        finishSource => finishSource
                            .TransitionTo(SourceFinishing)
                            .ThenAsync(context => FinishSource(context)),
                        done => done.ThenAsync(FinishRound).TransitionTo(Idle))),

            When(ParticipantStopped).ThenAsync(StopRound).TransitionTo(Stalled)
        );

        During(SourceFinishing,
            When(Ran)
                .ThenAsync(StoreFromRun)
                .IfElse(
                    context => Refused(context.Message),
                    refused => refused.ThenAsync(StallOnRefusal).TransitionTo(Stalled),
                    ok => ok.ThenAsync(FinishRound).TransitionTo(Idle)),

            When(ParticipantStopped).ThenAsync(StopRound).TransitionTo(Stalled)
        );

        During(Stalled,
            Ignore(Ran),
            Ignore(ParticipantStopped)
        );
    }

    private static bool Refused(TransferParticipantRan message) =>
        message.ProveExitCode is not null and not 0;

    private Task StallOnRefusal(BehaviorContext<TransferSaga, TransferParticipantRan> context) =>
        Stall(context,
            $"Module {context.Message.ModuleId} did not plan clean: " +
            (context.Message.Verdict ?? "it refused."));

    /// <summary>
    /// Starts the receiver, handing it the source's fragment and, when it does not need anything
    /// from the source, letting it prove in the same run.
    /// </summary>
    private static Task RunReceiver(BehaviorContext<TransferSaga, TransferParticipantRan> context)
    {
        var run = Run(context.Saga, TransferRole.Receiver, stopAfterMap: false);
        run.FragmentState = context.Message.FragmentState;
        run.FragmentMeta = context.Message.FragmentMeta;

        // When the source needs a value from the receiver, the receiver must prove first, so it is
        // given nothing and its own outputs come back for the source.
        if (!context.Saga.SourceNeedsValues)
            run.Outputs = context.Message.Outputs;

        return context.Publish(run);
    }

    /// <summary>Sends the source back to prove, now that the receiver has produced what it needs.</summary>
    private static Task FinishSource(BehaviorContext<TransferSaga, TransferParticipantRan> context)
    {
        var run = Run(context.Saga, TransferRole.Source, stopAfterMap: false);
        run.Outputs = context.Message.Outputs;
        return context.Publish(run);
    }

    private static TransferParticipantRunRequested Run(
        TransferSaga saga, TransferRole role, bool stopAfterMap) =>
        new()
        {
            CorrelationId = role == TransferRole.Source ? saga.SourceSagaId : saga.ReceiverSagaId,
            OrganizationId = saga.OrganizationId,
            JobId = saga.CurrentJobId!.Value,
            ProveRound = saga.ProveRound,
            ProveRef = role == TransferRole.Source ? saga.SourceProveRef : saga.ReceiverProveRef,
            Map = saga.Map!,
            StopAfterMap = stopAfterMap
        };
}
