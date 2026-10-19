// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Events.Jobs.Base;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.RippleProbes;
using SnapCd.Server.Core.StateMachine.Jobs.Activites.Finalization;
using SnapCd.Contracts;

namespace SnapCd.Server.Core.StateMachine.Jobs;

public partial class JobStateMachine<
    TSaga,
    TRequest,
    TResponseFailed,
    TResponseCompleted,
    TResponseCancelled,
    TGetDefinitiveRevisionRequested,
    TGetDefinitiveRevisionCompleted,
    TGetDefinitiveRevisionCancelled,
    TGetDefinitiveRevisionFaulted,
    TPolicyValidateRequested,
    TPolicyValidateCompleted,
    TPolicyValidateCancelled,
    TPolicyValidateFaulted,
    TOutputRequested,
    TOutputCompleted,
    TOutputCancelled,
    TOutputFaulted,
    TGetModuleRequested,
    TGetModuleCompleted,
    TGetModuleCancelled,
    TGetModuleFaulted,
    TInitRequested,
    TInitCompleted,
    TInitCancelled,
    TInitFaulted,
    TValidateRequested,
    TValidateCompleted,
    TValidateCancelled,
    TValidateFaulted,
    TVariablesRequested,
    TVariablesCompleted,
    TVariablesCancelled,
    TVariablesFaulted,
    TPlanRequested,
    TPlanCompleted,
    TPlanCancelled,
    TPlanFaulted,
    TApplyFromPlanRequested,
    TApplyFromPlanCompleted,
    TApplyFromPlanCancelled,
    TProbeRequested,
    TProbeCompleted,
    TProbeCancelled,
    TProbeFaulted,
    TCancelKillRequested,
    TDummyCancelKillCompleted,
    TCancelKillCompleted>
    where TSaga : JobSagaBase
    where TRequest : ModuleJobEventBase
    where TResponseFailed : ModuleJobEventCompletedBase, new()
    where TResponseCompleted : ModuleJobEventCompletedBase, new()
    where TResponseCancelled : ModuleJobEventCompletedBase, new()
    where TGetDefinitiveRevisionRequested : GetDefinitiveRevisionRequestedBase, new()
    where TGetDefinitiveRevisionCompleted : GetDefinitiveRevisionCompletedBase
    where TGetDefinitiveRevisionCancelled : StepResponseBase
    where TGetDefinitiveRevisionFaulted : StepFaultedBase
    where TPolicyValidateRequested : PolicyValidateRequestedBase, new()
    where TPolicyValidateCompleted : PolicyValidateCompletedBase
    where TPolicyValidateCancelled : StepResponseBase
    where TPolicyValidateFaulted : StepFaultedBase
    where TOutputRequested : OutputRequestedBase, new()
    where TOutputCompleted : StepResponseBase
    where TOutputCancelled : StepResponseBase
    where TOutputFaulted : StepFaultedBase
    where TGetModuleRequested : GetModuleRequestedBase, new()
    where TGetModuleCompleted : StepResponseBase
    where TGetModuleCancelled : StepResponseBase
    where TGetModuleFaulted : StepFaultedBase
    where TInitRequested : InitRequestedBase, new()
    where TInitCompleted : StepResponseBase
    where TInitCancelled : StepResponseBase
    where TInitFaulted : StepFaultedBase
    where TValidateRequested : ValidateRequestedBase, new()
    where TValidateCompleted : StepResponseBase
    where TValidateCancelled : StepResponseBase
    where TValidateFaulted : StepFaultedBase
    where TVariablesRequested : VariablesRequestedBase, new()
    where TVariablesCompleted : StepResponseBase
    where TVariablesCancelled : StepResponseBase
    where TVariablesFaulted : StepFaultedBase
    where TPlanRequested : StepRequestBase, new()
    where TPlanCompleted : PlanCompletedBase
    where TPlanCancelled : StepResponseBase
    where TPlanFaulted : StepFaultedBase, new()
    where TApplyFromPlanRequested : StepRequestBase, new()
    where TApplyFromPlanCompleted : ApplyResponseBase
    where TApplyFromPlanCancelled : StepResponseBase
    where TProbeRequested : ProbeRequestedBase, new()
    where TProbeCompleted : ProbeCompletedBase
    where TProbeCancelled : StepResponseBase
    where TProbeFaulted : StepFaultedBase
    where TCancelKillRequested : CancelKillRequestedBase, new()
    where TDummyCancelKillCompleted : class
    where TCancelKillCompleted : StepResponseBase
{
    public Event<ProbeWaveProgressedEvent> ProbeWaveProgressedEvent { get; } = null!;

    public State WaitingForDownstream { get; } = null!;

    private void Configure_DownstreamWait()
    {
        Event(() => ProbeWaveProgressedEvent, x => x.CorrelateById(y => y.Message.OriginModuleJobId));

        // A wave that moves on after this job stopped waiting is news about a job already past the
        // decision, so it must not fault an unhandled event.
        DuringAny(Ignore(ProbeWaveProgressedEvent));

        During(WaitingForDownstream,
            // The wave may already be settled by the time this job parks, and nothing re-raises
            // that. Entry prompts itself so the one arm below does the reading.
            When(WaitingForDownstream.Enter)
                .Publish(context => new ProbeWaveProgressedEvent
                {
                    OriginModuleJobId = context.Saga.CorrelationId,
                    OrganizationId = context.Saga.OrganizationId
                }),

            DealWithDownstreamWave(When(ProbeWaveProgressedEvent)),

            // Nothing is running on a runner while parked, so every cancellation ends the job at
            // once rather than waiting a step out.
            When(CancelModuleRequested)
                .Then(context => _logger.LogInformation(
                    "Cancelled while awaiting downstream findings for job {JobId}", context.Saga.CorrelationId))
                .Publish(context => new TResponseCancelled
                {
                    ModuleId = context.Saga.ModuleId,
                    OrganizationId = context.Saga.OrganizationId,
                    ModuleJobId = context.Saga.CorrelationId,
                    CancellationReason = CancellationReason.UserRequested
                })
                .Activity(x => x.OfType<CancelModuleJobActivity<TSaga, CancelModuleRequested>>())
                .TransitionTo(Cancelled)
                .Finalize(),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2),
            Ignore(RunnerReconnectedEvent)
        );
    }

    /// <summary>
    /// The two exits from the downstream question, used both when the apply first reaches it and
    /// every time the wave moves on: park, or go to approval.
    ///
    /// Read from the database each time rather than recorded when the wave was created. Consumers
    /// report while this job is already parked, which is the whole reason for parking rather than
    /// deciding once.
    /// </summary>
    private EventActivityBinder<TSaga, TMessage> DealWithDownstreamWave<TMessage>(
        EventActivityBinder<TSaga, TMessage> binder)
        where TMessage : class
    {
        return binder
            .IfElseAsync(
                IsWaveWaiting,
                wait => wait
                    .Then(context =>
                    {
                        context.Saga.WaitingSince ??= DateTime.UtcNow;
                        _logger.LogDebug(
                            "Propagation: job {CorrelationId} still waiting on a consumer",
                            context.Saga.CorrelationId);
                    })
                    .TransitionTo(WaitingForDownstream),
                settled => DealWithApprovalStatus(
                    settled.Then(context => context.Saga.WaitingSince = null),
                    true)
            );
    }

    /// <summary>
    /// Whether this job's wave still owes it an answer. A job that started no wave is not waiting,
    /// which is the ordinary case for almost every apply.
    ///
    /// This sits on the main apply path, immediately before the approval decision, so it answers
    /// "not waiting" when propagation is not wired or cannot be read. A host without propagation
    /// must still apply, and holding a job short of approval because a read failed would be worse
    /// than losing the finding.
    /// </summary>
    private async Task<bool> IsWaveWaiting<TMessage>(BehaviorContext<TSaga, TMessage> context)
        where TMessage : class
    {
        try
        {
            var provider = PipeExtensions.GetPayload<IServiceProvider>(context);
            var factory = provider.GetService<ProbeWaveProgressReaderFactory>();

            if (factory == null) return false;

            using var reader = factory.Create();
            var settlement = await reader.ReadForOriginJob(
                context.Saga.CorrelationId,
                context.Saga.OrganizationId);

            return settlement == ProbeWaveSettlement.Waiting;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Could not read propagation progress for job {CorrelationId}; continuing to approval",
                context.Saga.CorrelationId);

            return false;
        }
    }
}
