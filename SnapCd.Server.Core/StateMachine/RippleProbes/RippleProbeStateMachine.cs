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
using SnapCd.Server.Core.StateMachine.RippleProbes.Finalization;

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

    public State Completed { get; } = null!;
    public State Failed { get; } = null!;

    public RippleProbeStateMachine(ILogger<RippleProbeStateMachine> logger)
    {
        _logger = logger;

        InstanceState(x => x.CurrentState);
        SetCompletedWhenFinalized();

        Event(() => RippleProbeRequested, x => x.CorrelateById(y => y.Message.CorrelationId));

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
                    "Ripple probe completed for Module {ModuleId}, job {JobId}",
                    x.Saga.ModuleId, x.Saga.CorrelationId))
                .Publish(context => new RippleProbeCompleted
                {
                    ModuleId = context.Saga.ModuleId,
                    OrganizationId = context.Saga.OrganizationId,
                    ModuleJobId = context.Saga.CorrelationId
                })
                .Activity(x => x.OfType<CompleteRippleProbeJobActivity<RippleProbeRequested>>())
                .TransitionTo(Completed)
                .Finalize());
    }
}
