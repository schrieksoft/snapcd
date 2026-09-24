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
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Steps.Transfer;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.StateMachine.ManualJobs.Activities;
using SnapCd.Server.Core.StateMachine.ManualJobs.Finalization;
using SnapCd.Server.Core.StateMachine.Transfers.Migrate.Activities;

namespace SnapCd.Server.Core.StateMachine.Transfers.Migrate;

public partial class TransferMigrateStateMachine
{
    public Event<OutputsReevaluationRequestedEvent> OutputsModifiedEvent { get; } = null!;

    public State WaitingForOutputs { get; } = null!;

    /// <summary>
    /// A Module whose plan reads a value the other one produces cannot prove until that value
    /// exists. Transfers run in one direction, so only one side ever waits; the other passes
    /// straight through and eventually publishes what the first is waiting for.
    /// </summary>
    private void Configure_Outputs()
    {
        Event(() => OutputsModifiedEvent, x => x.CorrelateById(y => y.Message.ModuleJobId));

        During(WaitingForOutputs,
            DealWithOutputsStatus(When(OutputsModifiedEvent)),

            // Nothing is running on a runner here, so there is nothing to kill or wait out.
            When(CancelRequested)
                .Then(context => _logger.LogInformation(
                    "Transfer {TransferId}: cancelled while awaiting outputs for Module {ModuleId}",
                    context.Saga.TransferId, context.Saga.ModuleId))
                .Activity(x => x.OfType<CancelManualModuleJobActivity<TransferMigrateSaga, CancelManualModuleJobRequested>>())
                .TransitionTo(Failed)
                .Finalize()
        );
    }

    /// <summary>
    /// Proves when the values are there and parks when they are not. The same binder runs on entry
    /// and on every later output set, so a Module whose values already exist never waits.
    /// </summary>
    private EventActivityBinder<TransferMigrateSaga, TMessage> DealWithOutputsStatus<TMessage>(
        EventActivityBinder<TransferMigrateSaga, TMessage> binder, bool transition = false)
        where TMessage : class
    {
        return binder
            .Activity(x => x.OfType<TransferOutputsAvailableActivity<TMessage>>())
            .IfElse(
                x => x.Saga.HasOutputs,
                available => available
                    .Then(context => context.Saga.WaitingSince = null)
                    .Publish(context => Request<TransferMigrateProveRequested>(context.Saga))
                    .ThenAsync(context => RecordDispatched(context, "MigrateProve"))
                    .TransitionTo(MigrateProvePending),
                waiting => waiting
                    .If(
                        _ => transition,
                        parked => parked
                            .Then(context =>
                            {
                                context.Saga.WaitingSince = DateTime.UtcNow;
                                _logger.LogInformation(
                                    "Transfer {TransferId}: Module {ModuleId} is waiting on the other module's outputs",
                                    context.Saga.TransferId, context.Saga.ModuleId);
                            })
                            .TransitionTo(WaitingForOutputs)));
    }
}
