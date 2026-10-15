// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using System.Text.Json;
using MassTransit;
using Microsoft.Extensions.Logging;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.System;

namespace SnapCd.Server.Core.StateMachine.RippleProbes;

/// <summary>
/// A ripple probe's own state machine. Not a third closure of JobStateMachine: that generic is
/// closed over Apply and Destroy and most of its type parameters describe steps a probe never
/// reaches, since a probe stops at Plan and never applies, outputs or seeks approval.
/// </summary>
public partial class RippleProbeStateMachine : MassTransitStateMachine<RippleProbeSaga>
{
    private readonly ILogger<RippleProbeStateMachine> _logger;

    public Event<RippleProbeRequested> RippleProbeRequested { get; } = null!;

    public Event<RunnerReconnectedEvent> RunnerReconnectedEvent { get; } = null!;
    public Request<RippleProbeSaga, RippleProbeHeartbeatRequested, HeartbeatCompleted, HeartbeatFailed> HeartbeatRequested { get; } = null!;
    public Schedule<RippleProbeSaga, HeartbeatScheduled> HeartbeatScheduled { get; } = null!;

    public State Completed { get; } = null!;
    public State Failed { get; } = null!;

    public RippleProbeStateMachine(ILogger<RippleProbeStateMachine> logger)
    {
        _logger = logger;

        InstanceState(x => x.CurrentState);
        SetCompletedWhenFinalized();

        Event(() => RippleProbeRequested, x => x.CorrelateById(y => y.Message.CorrelationId));

        Event(() => RunnerReconnectedEvent, x => x
            .CorrelateBy((saga, context) =>
                saga.RunnerId == context.Message.RunnerId &&
                saga.RunnerInstanceName == context.Message.InstanceName &&
                saga.OrganizationId == context.Message.OrganizationId)
            .SelectId(context => Guid.NewGuid()));

        Request(() => HeartbeatRequested, x => x.HeartbeatRequestId, o => { o.Timeout = TimeSpan.FromSeconds(60); });

        Schedule(() => HeartbeatScheduled, saga => saga.HeartbeatScheduleTokenId, config =>
        {
            config.Delay = TimeSpan.FromSeconds(60);
            config.Received = e => e.CorrelateById(context => context.Message.CorrelationId);
        });

        Configure_Cancel();
        Configure_RippleProbeSelectRunnerInstance();
        Configure_RippleProbeGetModule();
        Configure_RippleProbeInit();
        Configure_RippleProbeValidate();
        Configure_RippleProbeProbe();

        // A cancel that arrives after the job ended has nothing to do, but must not fault: an
        // unhandled event is retried and then dead-lettered, which looks like a broken cancel.
        During(Completed, Ignore(RunnerReconnectedEvent), Ignore(CancelRippleProbeJobRequested));
        During(Failed, Ignore(RunnerReconnectedEvent), Ignore(CancelRippleProbeJobRequested));

        Initially(
            When(RippleProbeRequested)
                .Then(context =>
                {
                    context.Saga.CorrelationId = context.Message.CorrelationId;
                    context.Saga.ModuleId = context.Message.Declared.ModuleId;
                    context.Saga.OrganizationId = context.Message.Declared.OrganizationId;
                    context.Saga.DeclaredJson = JsonSerializer.Serialize(context.Message.Declared);
                    context.Saga.RunnerId = context.Message.Declared.RunnerId;
                    context.Saga.RunnerName = context.Message.Declared.RunnerName;
                    context.Saga.RunnerInstanceName = context.Message.Declared.RunnerInstanceName;
                    context.Saga.UpstreamModuleId = context.Message.UpstreamModuleId;
                    context.Saga.UpstreamModuleJobId = context.Message.UpstreamModuleJobId;
                    context.Saga.InputsJson = context.Message.InputsJson;
                })
                .Then(x => _logger.LogInformation(
                    "Ripple probe started for Module {ModuleId}, job {JobId}",
                    x.Saga.ModuleId, x.Saga.CorrelationId))
                .Publish(context => new SelectRunnerInstanceRequested
                {
                    RunnerId = context.Saga.RunnerId,
                    CorrelationId = context.Saga.CorrelationId,
                    OrganizationId = context.Saga.OrganizationId,
                    Declared = JsonSerializer.Deserialize<ResolvedModule>(context.Saga.DeclaredJson)!
                })
                .TransitionTo(RippleProbeSelectRunnerInstancePending));
    }
}
