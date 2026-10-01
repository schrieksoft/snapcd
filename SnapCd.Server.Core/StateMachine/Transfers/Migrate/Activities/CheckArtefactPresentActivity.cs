// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.Crud.Transfers;

namespace SnapCd.Server.Core.StateMachine.Transfers.Migrate.Activities;

/// <summary>
/// Asks the store whether what this half waits for is there yet, and prompts it if so. What is
/// waited for is the artefact existing, not a message saying so: the two halves race, so a message
/// can arrive before this half has parked, while the store answers whenever it is read.
/// </summary>
public class CheckArtefactPresentActivity(
    TransferArtefactService artefacts,
    ILogger<CheckArtefactPresentActivity> logger)
    : IStateMachineActivity<TransferMigrateSaga>
{
    public async Task Execute(
        BehaviorContext<TransferMigrateSaga> context,
        IBehavior<TransferMigrateSaga> next)
    {
        await Check(context.Saga, context);
        await next.Execute(context).ConfigureAwait(false);
    }

    private async Task Check(TransferMigrateSaga saga, PipeContext context)
    {
        if (saga.TransferId is not { } transferId) return;

        var publish = context.GetPayload<ConsumeContext>();

        // Which artefact this half waits for follows from the state it is in, not from its part:
        // the fragment always runs source to receiver, while the values run whichever way the
        // transfer's cross edge points.
        if (saga.CurrentState == nameof(TransferMigrateStateMachine.WaitingForOutputs))
        {
            if (await artefacts.HasReceiverOutputs(transferId, saga.OrganizationId))
                await publish.Publish(new TransferOutputsAvailable
                {
                    TransferId = transferId,
                    OrganizationId = saga.OrganizationId,
                    ProducedByModuleId = saga.CounterpartyModuleId
                });

            return;
        }

        // Already there, so this half prompts itself rather than waiting for a message that has
        // been and gone.
        if (await artefacts.HasSourceFragment(transferId, saga.OrganizationId))
            await publish.Publish(new TransferFragmentAvailable
            {
                TransferId = transferId,
                OrganizationId = saga.OrganizationId
            });
    }

    public async Task Execute<T>(
        BehaviorContext<TransferMigrateSaga, T> context,
        IBehavior<TransferMigrateSaga, T> next)
        where T : class
    {
        await Check(context.Saga, context);
        await next.Execute(context).ConfigureAwait(false);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TransferMigrateSaga, TException> context,
        IBehavior<TransferMigrateSaga> next)
        where TException : Exception
        => next.Faulted(context);

    public Task Faulted<T, TException>(
        BehaviorExceptionContext<TransferMigrateSaga, T, TException> context,
        IBehavior<TransferMigrateSaga, T> next)
        where T : class
        where TException : Exception
        => next.Faulted(context);

    public void Probe(ProbeContext context) => context.CreateScope("check-artefact-present");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}
