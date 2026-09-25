// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Steps.Transfer;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;
using SnapCd.Server.Core.StateMachine.ManualJobs.Activities;
using SnapCd.Server.Core.StateMachine.ManualJobs.Finalization;

namespace SnapCd.Server.Core.StateMachine.Transfers.Migrate;

public partial class TransferMigrateStateMachine
{
    public Event<ConsentDecided> ConsentDecidedEvent { get; } = null!;

    public State WaitingForConsent { get; } = null!;

    /// <summary>
    /// The Module that started a transfer waits here until the other agrees. It is a step rather
    /// than a state nothing shows, so the job appears on the page from the moment it is started.
    ///
    /// There is no timeout: an agreement nobody answers is ended by cancelling the job or by
    /// refusing it, both of which are deliberate acts.
    /// </summary>
    private void Configure_Consent()
    {
        // Correlated by the transfer, since the answer names that rather than this job.
        Event(() => ConsentDecidedEvent, x => x
            .CorrelateBy((saga, context) =>
                saga.TransferId == context.Message.TransferId &&
                saga.OrganizationId == context.Message.OrganizationId)
            .SelectId(context => Guid.NewGuid()));

        During(WaitingForConsent,
            When(ConsentDecidedEvent, context => context.Message.Granted)
                .ThenAsync(context => RecordCompleted(
                    context, "WaitForCounterpartyConsent", ManualJobStepStatus.Succeeded))
                .Activity(x => x.OfType<NotWaitingForConsentActivity<TransferMigrateSaga, ConsentDecided>>())
                .Then(context =>
                {
                    context.Saga.WaitingSince = null;
                    _logger.LogInformation(
                        "Transfer: Module {ModuleId} may go ahead", context.Saga.ModuleId);
                })
                .Publish(context => Request<TransferSelectRunnerInstanceRequested>(context.Saga))
                .ThenAsync(context => RecordDispatched(context, "SelectRunnerInstance"))
                .TransitionTo(SelectRunnerInstancePending),

            // Refused ends this side too: there is nothing for it to move into.
            When(ConsentDecidedEvent, context => !context.Message.Granted)
                .ThenAsync(context => RecordCompleted(
                    context, "WaitForCounterpartyConsent", ManualJobStepStatus.Refused))
                .Activity(x => x.OfType<NotWaitingForConsentActivity<TransferMigrateSaga, ConsentDecided>>())
                .Then(context => _logger.LogInformation(
                    "Transfer: {Counterparty} refused, so Module {ModuleId} writes nothing",
                    context.Saga.CounterpartyModuleId, context.Saga.ModuleId))
                .ThenJobFailed().TransitionTo(Failed).Finalize(),

            // Nothing is on a runner yet, so there is nothing to kill or wait out.
            When(CancelRequested)
                .ThenAsync(context => RecordCompleted(
                    context, "WaitForCounterpartyConsent", ManualJobStepStatus.Faulted,
                    "Cancelled while waiting."))
                .Activity(x => x.OfType<NotWaitingForConsentActivity<TransferMigrateSaga, CancelManualModuleJobRequested>>())
                .Then(context => _logger.LogInformation(
                    "Transfer: cancelled while Module {ModuleId} waited to be agreed to",
                    context.Saga.ModuleId))
                .Activity(x => x.OfType<CancelManualModuleJobActivity<TransferMigrateSaga, CancelManualModuleJobRequested>>())
                .TransitionTo(Failed)
                .Finalize()
        );
    }
}
