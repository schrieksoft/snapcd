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
    /// A prove round. The two Modules prepare independently - neither needs anything from the other
    /// to check out and plan - and only then does the sequence that couples them begin: the source
    /// pulls its state and writes the fragment, the receiver applies it, and each proves in an order
    /// that depends on whether one needs a value the other produces.
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
                    context.Saga.StallReason = null;
                    context.Saga.ProvesFirstModuleId = null;

                    _logger.LogInformation(
                        "Transfer {TransferId}: prove round {Round} starting",
                        context.Saga.TransferId, context.Saga.ProveRound);

                    var (source, receiver) = SagaIds(context.Saga);

                    context.Publish(new TransferParticipantPrepareRequested
                    {
                        CorrelationId = source,
                        OrganizationId = context.Saga.OrganizationId,
                        JobId = context.Message.JobId,
                        ProveRound = context.Saga.ProveRound,
                        ProveRef = context.Message.SourceProveRef
                    });

                    context.Publish(new TransferParticipantPrepareRequested
                    {
                        CorrelationId = receiver,
                        OrganizationId = context.Saga.OrganizationId,
                        JobId = context.Message.JobId,
                        ProveRound = context.Saga.ProveRound,
                        ProveRef = context.Message.ReceiverProveRef
                    });
                })
                .TransitionTo(Preparing)
        );

        During(Preparing,
            // Both Modules answer this, so the first reply waits: the step rows say who has
            // finished, and asking them is idempotent if a reply is delivered twice.
            When(Prepared)
                .ThenAsync(async context =>
                {
                    // Cleared each time so the transition below reads this reply, not the last one.
                    context.Saga.AdvanceRound = false;

                    if (context.Message.TotalChangedCount != 0)
                    {
                        await Stall(context,
                            $"Module {context.Message.ModuleId} plans {context.Message.TotalChangedCount} changes; " +
                            "a transfer proves against a clean plan.");
                        return;
                    }

                    if (!await BothFinished(context, "Plan")) return;

                    // The source goes first: its map writes the fragment the receiver needs.
                    await context.Publish(new TransferParticipantMapRequested
                    {
                        CorrelationId = context.Saga.SourceSagaId,
                        OrganizationId = context.Saga.OrganizationId,
                        Map = context.Saga.Map!
                    });

                    context.Saga.AdvanceRound = true;
                })
                .IfElse(
                    context => context.Saga.StallReason != null,
                    stalled => stalled.TransitionTo(Stalled),
                    // Only the reply that found both Modules finished moves the round on; the
                    // first one to arrive leaves it where it is.
                    otherwise => otherwise.If(
                        context => context.Saga.AdvanceRound,
                        advance => advance.TransitionTo(SourceMapping))),

            When(ParticipantStopped).ThenAsync(StopRound).TransitionTo(Stalled)
        );

        During(SourceMapping,
            When(Mapped)
                .ThenAsync(async context =>
                {
                    // The fragment is state, so it is stored encrypted rather than carried on the
                    // message any further than it has to be.
                    await StoreFragment(context);

                    context.Saga.ProvesFirstModuleId = DecideOrder(context.Saga, context.Message.NeedsValuesFrom);

                    await context.Publish(new TransferParticipantMapRequested
                    {
                        CorrelationId = context.Saga.ReceiverSagaId,
                        OrganizationId = context.Saga.OrganizationId,
                        Map = context.Saga.Map!,
                        FragmentState = context.Message.FragmentState,
                        FragmentMeta = context.Message.FragmentMeta
                    });
                })
                .TransitionTo(ReceiverMapping),

            When(ParticipantStopped).ThenAsync(StopRound).TransitionTo(Stalled)
        );

        During(ReceiverMapping,
            When(Mapped)
                .ThenAsync(async context =>
                {
                    var first = context.Saga.ProvesFirstModuleId;

                    if (first == null)
                    {
                        // Neither Module needs anything from the other, so both prove at once.
                        await Prove(context, context.Saga.SourceSagaId);
                        await Prove(context, context.Saga.ReceiverSagaId);
                        context.Saga.AdvanceRound = true;
                        return;
                    }

                    await Prove(context, first == context.Saga.SourceModuleId
                        ? context.Saga.SourceSagaId
                        : context.Saga.ReceiverSagaId);

                    context.Saga.AdvanceRound = false;
                })
                .IfElse(
                    context => context.Saga.ProvesFirstModuleId == null,
                    together => together.TransitionTo(ProvingBoth),
                    ordered => ordered.TransitionTo(ProvingFirst)),

            When(ParticipantStopped).ThenAsync(StopRound).TransitionTo(Stalled)
        );

        During(ProvingFirst,
            When(Proved)
                .ThenAsync(async context =>
                {
                    if (context.Message.ExitCode != 0)
                    {
                        await Stall(context,
                            $"Module {context.Message.ModuleId} did not plan clean: " +
                            (context.Message.Verdict ?? "it refused."));
                        return;
                    }

                    // The values this one produced are what the other one was waiting for.
                    await StoreOutputs(context);

                    var second = context.Message.ModuleId == context.Saga.SourceModuleId
                        ? context.Saga.ReceiverSagaId
                        : context.Saga.SourceSagaId;

                    await Prove(context, second, context.Message.Outputs);
                })
                .IfElse(
                    context => context.Saga.StallReason != null,
                    stalled => stalled.TransitionTo(Stalled),
                    otherwise => otherwise.TransitionTo(ProvingSecond)),

            When(ParticipantStopped).ThenAsync(StopRound).TransitionTo(Stalled)
        );

        During(ProvingSecond, ProvingBoth,
            When(Proved)
                .ThenAsync(async context =>
                {
                    context.Saga.AdvanceRound = false;

                    if (context.Message.ExitCode != 0)
                    {
                        await Stall(context,
                            $"Module {context.Message.ModuleId} did not plan clean: " +
                            (context.Message.Verdict ?? "it refused."));
                        return;
                    }

                    if (!await BothFinished(context, "MigrateProve")) return;

                    _logger.LogInformation(
                        "Transfer {TransferId}: round {Round} proved green",
                        context.Saga.TransferId, context.Saga.ProveRound);

                    await Finish(context);
                    context.Saga.AdvanceRound = true;
                })
                .IfElse(
                    context => context.Saga.StallReason != null,
                    stalled => stalled.TransitionTo(Stalled),
                    otherwise => otherwise.If(
                        context => context.Saga.AdvanceRound,
                        done => done.TransitionTo(Idle))),

            When(ParticipantStopped).ThenAsync(StopRound).TransitionTo(Stalled)
        );

        // A stalled round is over; the next one starts from here as it would from Idle.
        During(Stalled,
            When(ProveRoundRequested).ThenAsync(context => Task.CompletedTask),
            Ignore(Prepared),
            Ignore(Mapped),
            Ignore(Proved),
            Ignore(ParticipantStopped)
        );
    }

    /// <summary>Asks one Module to prove, with whatever values it needs from the other.</summary>
    private static Task Prove<TMessage>(
        BehaviorContext<TransferSaga, TMessage> context,
        Guid participantSagaId,
        Dictionary<string, string>? outputs = null)
        where TMessage : class =>
        context.Publish(new TransferParticipantProveRequested
        {
            CorrelationId = participantSagaId,
            OrganizationId = context.Saga.OrganizationId,
            Map = context.Saga.Map!,
            Outputs = outputs ?? new Dictionary<string, string>()
        });

    /// <summary>
    /// Which Module proves first: the one producing a value the other needs, so the value exists
    /// when the other plans. Null when neither needs anything, and both can prove at once.
    /// </summary>
    private static Guid? DecideOrder(TransferSaga saga, List<string> sourceNeedsValuesFrom) =>
        sourceNeedsValuesFrom.Count > 0 ? saga.ReceiverModuleId : null;
}
