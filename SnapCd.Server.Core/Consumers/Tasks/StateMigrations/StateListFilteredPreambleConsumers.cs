// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.AspNetCore.SignalR;
using SnapCd.Contracts.Constants;
using SnapCd.Server.Core.Consumers.Tasks.Builders;
using SnapCd.Server.Core.Consumers.Tasks.ManualJobs;
using SnapCd.Server.Core.Consumers.Tasks.Transfers;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Factories;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.Crud.Transfers;

namespace SnapCd.Server.Core.Consumers.Tasks.StateMigrations;

public class StateListFilteredSelectRunnerInstanceConsumer(
    ILogger<StateListFilteredSelectRunnerInstanceConsumer> logger,
    RunnerSelectionService runnerSelection,
    RunnerJobAuthorizationService authorizationService)
    : ManualSelectRunnerInstanceConsumer<
        StateListFilteredSelectRunnerInstanceRequested,
        StateListFilteredSelectRunnerInstanceCompleted,
        StateListFilteredSelectRunnerInstanceFaulted>(logger, runnerSelection, authorizationService)
{
    protected override void SetInstanceName(
        StateListFilteredSelectRunnerInstanceCompleted completed, string instanceName)
        => completed.RunnerInstanceName = instanceName;
}

public class StateListFilteredGetModuleConsumer(
    ILogger<StateListFilteredGetModuleConsumer> logger,
    IHubContext<RunnerHub> hubContext,
    RunnerSelectionService runnerSelection)
    : TransferStepConsumer<StateListFilteredGetModuleRequested, StateListFilteredGetModuleFaulted>(
        logger, hubContext, runnerSelection)
{
    protected override string Endpoint => RunnerEndpoints.GetModule;

    protected override Task<object> BuildPayload(
        ConsumeContext<StateListFilteredGetModuleRequested> context, Guid jobId)
    {
        var msg = context.Message;
        var ordinary = StepRequestBuilders.GetModule(jobId, msg.OrganizationId, msg.Declared);

        if (!string.IsNullOrWhiteSpace(msg.SourceRevisionOverride))
            ordinary.SourceRevision = msg.SourceRevisionOverride;

        return Task.FromResult<object>(ordinary);
    }
}

public class StateListFilteredInitConsumer(
    ILogger<StateListFilteredInitConsumer> logger,
    IHubContext<RunnerHub> hubContext,
    RunnerSelectionService runnerSelection,
    ParamResolverFactory paramResolverFactory)
    : TransferStepConsumer<StateListFilteredInitRequested, StateListFilteredInitFaulted>(
        logger, hubContext, runnerSelection)
{
    protected override string Endpoint => RunnerEndpoints.Init;

    protected override async Task<object> BuildPayload(
        ConsumeContext<StateListFilteredInitRequested> context, Guid jobId)
    {
        var msg = context.Message;
        var resolvedEnvVars = await StepRequestBuilders.ResolveInitEnvVars(
            paramResolverFactory, jobId, msg.OrganizationId, msg.Declared, logger);

        return StepRequestBuilders.Init(jobId, msg.OrganizationId, msg.Declared, resolvedEnvVars);
    }
}
