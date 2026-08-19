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
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Jobs.Module;
using SnapCd.Server.Core.Events.Runners;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Events.Steps.SplitMonolith;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;
using SnapCd.Server.Core.StateMachine.Jobs.Activites;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;

namespace SnapCd.Server.Core.StateMachine.SplitMonolith;

/// <summary>
/// Drives a SplitMonolith manual job. Separate from JobStateMachine because that machine's shape is
/// fixed — it always ends Plan, PolicyValidate, ApplyFromPlan, Output — and a split diverges after
/// Plan into steps it knows nothing about. The per-step waiting/pending pair, its liveness checks
/// and its cancellation handling are duplicated here rather than shared, so changes to the
/// deployment pipeline cannot alter how a split behaves.
/// </summary>
public partial class SplitMonolithStateMachine : MassTransitStateMachine<SplitMonolithSaga>
{
    private readonly ILogger<SplitMonolithStateMachine> _logger;

    // Job-level entry point
    public Event<SplitMonolithRequested> SplitRequested { get; } = null!;

    // Liveness
    public Event<RunnerReconnectedEvent> RunnerReconnectedEvent { get; } = null!;
    public Request<SplitMonolithSaga, HeartbeatRequested, HeartbeatCompleted, HeartbeatFailed> HeartbeatRequested { get; } = null!;
    public Schedule<SplitMonolithSaga, HeartbeatScheduled> HeartbeatScheduled { get; } = null!;

    // Terminal
    public State Completed { get; } = null!;
    public State Failed { get; } = null!;

    public SplitMonolithStateMachine(ILogger<SplitMonolithStateMachine> logger)
    {
        _logger = logger;

        InstanceState(x => x.CurrentState);
        SetCompletedWhenFinalized();

        Event(() => SplitRequested, x => x.CorrelateById(y => y.Message.CorrelationId));

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

        Initially(
            When(SplitRequested)
                .Then(context =>
                {
                    context.Saga.CorrelationId = context.Message.CorrelationId;
                    context.Saga.ModuleId = context.Message.Declared.ModuleId;
                    context.Saga.OrganizationId = context.Message.Declared.OrganizationId;
                    context.Saga.DeclaredJson = JsonSerializer.Serialize(context.Message.Declared);
                    context.Saga.RunnerId = context.Message.Declared.RunnerId;
                    context.Saga.RunnerName = context.Message.Declared.RunnerName;
                    context.Saga.RunnerInstanceName = context.Message.Declared.RunnerInstanceName;
                    context.Saga.ApprovalTimeoutMinutes = context.Message.Declared.ApprovalTimeoutMinutes;
                    context.Saga.OutDirectory = context.Message.OutDirectory;
                    context.Saga.RootDirectory = context.Message.RootDirectory;
                    context.Saga.Overwrite = context.Message.Overwrite;
                })
                .Publish(context => new SelectRunnerInstanceRequested
                {
                    RunnerId = context.Saga.RunnerId,
                    CorrelationId = context.Saga.CorrelationId,
                    OrganizationId = context.Saga.OrganizationId,
                    Declared = JsonSerializer.Deserialize<ResolvedModule>(context.Saga.DeclaredJson)!
                })
                .TransitionTo(SelectRunnerInstancePending)
        );

        Configure_SelectRunnerInstance();
        Configure_GetModule();
        Configure_Init();
        Configure_Validate();
        Configure_Plan();
        Configure_RefactorVerify();
        Configure_MigrateMap();
        Configure_MigrateProve();
        Configure_Approval();
        Configure_MigrateRun();
        Configure_MigrateVerify();

        During(Completed, Ignore(RunnerReconnectedEvent));
        During(Failed, Ignore(RunnerReconnectedEvent));
        During(Cancelled, Ignore(RunnerReconnectedEvent));
    }
}
