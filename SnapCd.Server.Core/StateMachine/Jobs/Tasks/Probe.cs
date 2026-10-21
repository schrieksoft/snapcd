// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Diagnostics;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Jobs.Base;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.RippleProbes;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Contracts;
using SnapCd.Server.Core.StateMachine.Jobs.Activites;
using SnapCd.Server.Core.StateMachine.Jobs.Activites.Finalization;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;
using SnapCd.Server.Core.Events.Jobs.Module;

namespace SnapCd.Server.Core.StateMachine.Jobs;

/// <summary>
/// State names a reader outside the state machine has to match on. JobStateMachine is generic
/// over its whole message set, so its State members cannot be reached by nameof from elsewhere.
/// </summary>
public static class JobStateNames
{
    /// <summary>Where a job decides whether it owes a wave a probe.</summary>
    public const string WaitingForProbeSignals = "WaitingForProbeSignals";
}

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
    public Event<TProbeRequested> ProbeRequested { get; } = null!;
    public Event<TProbeCompleted> ProbeCompleted { get; } = null!;
    public Event<TProbeCancelled> ProbeCancelled { get; } = null!;
    public Event<TProbeFaulted> ProbeFaulted { get; } = null!;

    public Event<ProbeSignalsReevaluationRequestedEvent> ProbeSignalsModifiedEvent { get; } = null!;

    public State WaitingForProbeSignals { get; } = null!;
    public State ProbeWaitingForRunner { get; } = null!;
    public State ProbePending { get; } = null!;

    private void Configure_Probe()
    {
        // The readiness check matches a running job's persisted state against this name.
        Debug.Assert(
            nameof(WaitingForProbeSignals) == JobStateNames.WaitingForProbeSignals,
            "JobStateNames.WaitingForProbeSignals no longer matches the state it names.");

        Event(() => ProbeRequested, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => ProbeCompleted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => ProbeCancelled, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => ProbeFaulted, x => x.CorrelateById(y => y.Message.CorrelationId));
        Event(() => ProbeSignalsModifiedEvent, x => x.CorrelateById(y => y.Message.ModuleJobId));

        // The entry prompt goes through the bus, so it can arrive after the state it was sent
        // from has been left. An unhandled event faults the saga, so every other state ignores it.
        DuringAny(Ignore(ProbeSignalsModifiedEvent));

        During(WaitingForProbeSignals,
            // The decision needs a database read, which an Enter arm cannot branch a transition
            // on, so entry prompts itself and the arm below does the reading. The job passes
            // through this state rather than resting in it.
            When(WaitingForProbeSignals.Enter)
                .Publish(context => new ProbeSignalsReevaluationRequestedEvent
                {
                    ModuleId = context.Saga.ModuleId,
                    ModuleJobId = context.Saga.CorrelationId
                }),

            DealWithProbeReadiness(When(ProbeSignalsModifiedEvent)),

            // Reached only if the prompt is still in flight when a cancel arrives. Nothing is
            // running on a runner here, so the cancel ends the job at once.
            When(CancelModuleRequested)
                .Then(context => _logger.LogInformation(
                    "Cancelled while awaiting probe signals for job {JobId}", context.Saga.CorrelationId))
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

        During(ProbeWaitingForRunner,
            When(ProbeWaitingForRunner.Enter)
                .Activity(x => x.OfType<CheckRunnerConnectionActivity<TSaga, object>>()),
            When(RunnerReconnectedEvent)
                .Then(context =>
                {
                    _logger.LogInformation(
                        "Probe: Runner reconnected for job {CorrelationId}, retrying send",
                        context.Saga.CorrelationId);
                })
                .Activity(x => x.OfType<SendProbeStepToRunnerActivity<TSaga, RunnerReconnectedEvent, TProbeRequested>>())
                .IfElse(
                    context => context.Saga.PreviousStateBeforeWaiting != null,
                    whenTrue => whenTrue
                        .Then(context =>
                        {
                            _logger.LogWarning(
                                "Probe: Runner disconnected again for job {CorrelationId}, staying in waiting state",
                                context.Saga.CorrelationId);
                        }),
                    whenFalse => whenFalse
                        .Then(context =>
                        {
                            context.Saga.WaitingSince = null;
                            _logger.LogDebug(
                                "Probe: Successfully sent for job {CorrelationId}, transitioning to pending",
                                context.Saga.CorrelationId);
                        })
                        .Schedule(HeartbeatScheduled,
                            context => new HeartbeatScheduled { CorrelationId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId })
                        .TransitionTo(ProbePending)
                ),
            When(CancelModuleRequested)
                .IfCancelKill<TSaga, TResponseCancelled, CancelModuleRequested, TCancelKillRequested, TDummyCancelKillCompleted>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelAfterCurrent<TSaga, CancelModuleRequested>(_logger, CancellingAfterCurrent),
            Ignore(HeartbeatScheduled.Received),
            Ignore(HeartbeatRequested.Completed),
            Ignore(HeartbeatRequested.Completed2)
        );

        During(ProbePending,
            // The apply has already happened, so a probe that does not answer cannot make the job
            // wrong. Every arm below continues to Output rather than failing or cancelling the job.
            // Several waves can be open on one Module at once, each predicting different values
            // for its inputs, so each is owed a probe of its own rather than one answer shared
            // between them. The signals just recorded close this wave's question; if another is
            // still owed, probe again before reading the outputs.
            When(ProbeCompleted)
                .IfElseAsync(
                    AnotherWaveIsOwed,
                    again => DealWithProbeReadiness(again),
                    done => done
                        .Activity(x => x.OfType<SendToRunnerActivity<TSaga, TProbeCompleted, TOutputRequested>>())
                        .TransitionTo(OutputPending)),
            // A faulted probe is as finished as a completed one, so any wave still owed is probed
            // next. The faulted wave itself drops out: its attempt row belongs to this job.
            When(ProbeFaulted)
                .Then(context =>
                {
                    _logger.LogWarning(
                        "Probe: faulted for job {CorrelationId}. The apply itself succeeded.",
                        context.Saga.CorrelationId);
                })
                .IfElseAsync(
                    AnotherWaveIsOwed,
                    again => DealWithProbeReadiness(again),
                    done => done
                        .Activity(x => x.OfType<SendToRunnerActivity<TSaga, TProbeFaulted, TOutputRequested>>())
                        .TransitionTo(OutputPending)),
            When(ProbeCancelled)
                .ThenCancelled<TSaga, TResponseCancelled, TProbeCancelled>(Cancelled),
            When(HeartbeatScheduled.Received)
                .ThenHeartbeatScheduled(HeartbeatRequested),
            When(HeartbeatRequested.Completed)
                .ThenHeartbeatCompleted(HeartbeatScheduled),
            When(HeartbeatRequested.Completed2)
                .ThenJobTimedOut<TSaga, TResponseFailed>(Failed),
            When(CancelModuleRequested)
                .IfCancelKill<TSaga, TResponseCancelled, CancelModuleRequested, TCancelKillRequested, TDummyCancelKillCompleted>(_logger, CancelKillRequested, CancellingImmediateKill, Cancelled)
                .IfCancelAfterCurrent<TSaga, CancelModuleRequested>(_logger, CancellingAfterCurrent),
            Ignore(RunnerReconnectedEvent)
        );
    }

    /// <summary>
    /// The two exits from the probe decision: probe, or read the outputs.
    ///
    /// Decided once, on arrival. A probe runs only if every condition for it is already met at
    /// this moment; an upstream that has not yet reported means no probe on this apply rather
    /// than an apply held open until it does. The apply has already succeeded by the time this
    /// runs, and holding a finished apply open to wait for information about someone else's
    /// Module is a cost paid by the wrong job.
    /// </summary>
    private EventActivityBinder<TSaga, TMessage> DealWithProbeReadiness<TMessage>(
        EventActivityBinder<TSaga, TMessage> binder)
        where TMessage : class
    {
        return binder
            .IfElseAsync(
                IsReadyToProbe,
                probe => probe
                    .Activity(x => x.OfType<SendProbeStepToRunnerActivity<TSaga, TMessage, TProbeRequested>>())
                    .IfElse(
                        context => context.Saga.PreviousStateBeforeWaiting != null,
                        whenTrue => whenTrue
                            .Then(context =>
                            {
                                context.Saga.WaitingSince = DateTime.UtcNow;
                                _logger.LogWarning(
                                    "Probe: Runner disconnected for job {CorrelationId}, entering waiting state",
                                    context.Saga.CorrelationId);
                            })
                            .TransitionTo(ProbeWaitingForRunner),
                        whenFalse => whenFalse
                            .Schedule(HeartbeatScheduled,
                                context => new HeartbeatScheduled { CorrelationId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId })
                            .TransitionTo(ProbePending)
                    ),
                skip => skip
                    .Activity(x => x.OfType<SendToRunnerActivity<TSaga, TMessage, TOutputRequested>>())
                    .IfElse(
                        context => context.Saga.PreviousStateBeforeWaiting != null,
                        whenTrue => whenTrue
                            .Then(context =>
                            {
                                context.Saga.WaitingSince = DateTime.UtcNow;
                                _logger.LogWarning(
                                    "Probe: Runner disconnected for job {CorrelationId}, entering waiting state",
                                    context.Saga.CorrelationId);
                            })
                            .TransitionTo(OutputWaitingForRunner),
                        whenFalse => whenFalse
                            .Schedule(HeartbeatScheduled,
                                context => new HeartbeatScheduled { CorrelationId = context.Saga.CorrelationId, OrganizationId = context.Saga.OrganizationId })
                            .TransitionTo(OutputPending)
                    )
            );
    }

    /// <summary>
    /// Whether a probe should run on this apply. Everything that is not a clear yes is a no: a
    /// Module with no wave, an upstream that has not answered, and a resolved wave carrying
    /// nothing this Module reads all mean the outputs are read and the job finishes.
    /// </summary>
    /// <summary>
    /// Whether this Module still owes a probe to a wave it has not reported in. Recomputed rather
    /// than counted down, so a wave that opened or ended mid-apply is picked up as it stands.
    /// </summary>
    private static async Task<bool> AnotherWaveIsOwed<TMessage>(BehaviorContext<TSaga, TMessage> context)
        where TMessage : class
    {
        var provider = PipeExtensions.GetPayload<IServiceProvider>(context);
        using var inputs = provider.GetRequiredService<ProbeInputsBuilderFactory>().Create();

        var waves = await inputs.FindWavesToProbeFor(
            context.Saga.ModuleId, context.Saga.OrganizationId, context.Saga.CorrelationId);
        return waves.Count > 0;
    }

    private static async Task<bool> IsReadyToProbe<TMessage>(BehaviorContext<TSaga, TMessage> context)
        where TMessage : class
    {
        var provider = PipeExtensions.GetPayload<IServiceProvider>(context);
        using var reader = provider.GetRequiredService<ProbeReadinessReaderFactory>().Create();

        var readiness = await reader.Read(context.Saga.ModuleId, context.Saga.OrganizationId);
        return readiness == ProbeReadiness.Probe;
    }
}
