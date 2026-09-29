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
using SnapCd.Server.Core.Consumers.Tasks.Transfers;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Factories;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Services;

namespace SnapCd.Server.Core.Consumers.Tasks.StateMigrations;

/// <summary>
/// Checks out the code the job runs against. One dispatch serves every manual family, because the
/// runner does one checkout; the faulted reply is the asking family's own, so a dispatch that fails
/// reaches the saga that asked.
/// </summary>
public abstract class StateMigrationGetModuleConsumer<TRequested, TFaulted>(
    ILogger logger,
    IHubContext<RunnerHub> hubContext,
    RunnerSelectionService runnerSelection)
    : TransferStepConsumer<TRequested, TFaulted>(logger, hubContext, runnerSelection)
    where TRequested : StateMigrationGetModuleRequestedBase
    where TFaulted : StateMigrationStepFaultedBase, new()
{
    protected override Task<object> BuildPayload(
        ConsumeContext<TRequested> context, Guid jobId)
    {
        var msg = context.Message;
        var ordinary = StepRequestBuilders.GetModule(jobId, msg.OrganizationId, msg.Declared);

        if (!string.IsNullOrWhiteSpace(msg.SourceRevisionOverride))
            ordinary.SourceRevision = msg.SourceRevisionOverride;

        return Task.FromResult<object>(ordinary);
    }
}

/// <summary>Initialises the backend, so the state is reachable.</summary>
public abstract class StateMigrationInitConsumer<TRequested, TFaulted>(
    ILogger logger,
    IHubContext<RunnerHub> hubContext,
    RunnerSelectionService runnerSelection,
    ParamResolverFactory paramResolverFactory)
    : TransferStepConsumer<TRequested, TFaulted>(logger, hubContext, runnerSelection)
    where TRequested : StateMigrationStepRequestBase
    where TFaulted : StateMigrationStepFaultedBase, new()
{
    protected override async Task<object> BuildPayload(
        ConsumeContext<TRequested> context, Guid jobId)
    {
        var msg = context.Message;
        var resolvedEnvVars = await StepRequestBuilders.ResolveInitEnvVars(
            paramResolverFactory, jobId, msg.OrganizationId, msg.Declared, logger);

        return StepRequestBuilders.Init(jobId, msg.OrganizationId, msg.Declared, resolvedEnvVars);
    }
}

// One concrete set per family: the reply a dispatch failure raises has to reach the saga
// that asked for the step, so each family names its own.

public class StateListFilteredSelectRunnerInstanceConsumer(
    ILogger<StateListFilteredSelectRunnerInstanceConsumer> logger,
    RunnerSelectionService runnerSelection,
    RunnerJobAuthorizationService authorizationService)
    : StateMigrationSelectRunnerInstanceConsumer<
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
    : StateMigrationGetModuleConsumer<StateListFilteredGetModuleRequested, StateListFilteredGetModuleFaulted>(logger, hubContext, runnerSelection)
{
    protected override string Endpoint => RunnerEndpoints.StateListFilteredGetModule;
}

public class StateListFilteredInitConsumer(
    ILogger<StateListFilteredInitConsumer> logger,
    IHubContext<RunnerHub> hubContext,
    RunnerSelectionService runnerSelection,
    ParamResolverFactory paramResolverFactory)
    : StateMigrationInitConsumer<StateListFilteredInitRequested, StateListFilteredInitFaulted>(
        logger, hubContext, runnerSelection, paramResolverFactory)
{
    protected override string Endpoint => RunnerEndpoints.StateListFilteredInit;
}

public class MoveSelectRunnerInstanceConsumer(
    ILogger<MoveSelectRunnerInstanceConsumer> logger,
    RunnerSelectionService runnerSelection,
    RunnerJobAuthorizationService authorizationService)
    : StateMigrationSelectRunnerInstanceConsumer<
        MoveSelectRunnerInstanceRequested,
        MoveSelectRunnerInstanceCompleted,
        MoveSelectRunnerInstanceFaulted>(logger, runnerSelection, authorizationService)
{
    protected override void SetInstanceName(
        MoveSelectRunnerInstanceCompleted completed, string instanceName)
        => completed.RunnerInstanceName = instanceName;
}

public class MoveGetModuleConsumer(
    ILogger<MoveGetModuleConsumer> logger,
    IHubContext<RunnerHub> hubContext,
    RunnerSelectionService runnerSelection)
    : StateMigrationGetModuleConsumer<MoveGetModuleRequested, MoveGetModuleFaulted>(logger, hubContext, runnerSelection)
{
    protected override string Endpoint => RunnerEndpoints.MoveGetModule;
}

public class MoveInitConsumer(
    ILogger<MoveInitConsumer> logger,
    IHubContext<RunnerHub> hubContext,
    RunnerSelectionService runnerSelection,
    ParamResolverFactory paramResolverFactory)
    : StateMigrationInitConsumer<MoveInitRequested, MoveInitFaulted>(
        logger, hubContext, runnerSelection, paramResolverFactory)
{
    protected override string Endpoint => RunnerEndpoints.MoveInit;
}

public class ImportSelectRunnerInstanceConsumer(
    ILogger<ImportSelectRunnerInstanceConsumer> logger,
    RunnerSelectionService runnerSelection,
    RunnerJobAuthorizationService authorizationService)
    : StateMigrationSelectRunnerInstanceConsumer<
        ImportSelectRunnerInstanceRequested,
        ImportSelectRunnerInstanceCompleted,
        ImportSelectRunnerInstanceFaulted>(logger, runnerSelection, authorizationService)
{
    protected override void SetInstanceName(
        ImportSelectRunnerInstanceCompleted completed, string instanceName)
        => completed.RunnerInstanceName = instanceName;
}

public class ImportGetModuleConsumer(
    ILogger<ImportGetModuleConsumer> logger,
    IHubContext<RunnerHub> hubContext,
    RunnerSelectionService runnerSelection)
    : StateMigrationGetModuleConsumer<ImportGetModuleRequested, ImportGetModuleFaulted>(logger, hubContext, runnerSelection)
{
    protected override string Endpoint => RunnerEndpoints.ImportGetModule;
}

public class ImportInitConsumer(
    ILogger<ImportInitConsumer> logger,
    IHubContext<RunnerHub> hubContext,
    RunnerSelectionService runnerSelection,
    ParamResolverFactory paramResolverFactory)
    : StateMigrationInitConsumer<ImportInitRequested, ImportInitFaulted>(
        logger, hubContext, runnerSelection, paramResolverFactory)
{
    protected override string Endpoint => RunnerEndpoints.ImportInit;
}

public class RemoveSelectRunnerInstanceConsumer(
    ILogger<RemoveSelectRunnerInstanceConsumer> logger,
    RunnerSelectionService runnerSelection,
    RunnerJobAuthorizationService authorizationService)
    : StateMigrationSelectRunnerInstanceConsumer<
        RemoveSelectRunnerInstanceRequested,
        RemoveSelectRunnerInstanceCompleted,
        RemoveSelectRunnerInstanceFaulted>(logger, runnerSelection, authorizationService)
{
    protected override void SetInstanceName(
        RemoveSelectRunnerInstanceCompleted completed, string instanceName)
        => completed.RunnerInstanceName = instanceName;
}

public class RemoveGetModuleConsumer(
    ILogger<RemoveGetModuleConsumer> logger,
    IHubContext<RunnerHub> hubContext,
    RunnerSelectionService runnerSelection)
    : StateMigrationGetModuleConsumer<RemoveGetModuleRequested, RemoveGetModuleFaulted>(logger, hubContext, runnerSelection)
{
    protected override string Endpoint => RunnerEndpoints.RemoveGetModule;
}

public class RemoveInitConsumer(
    ILogger<RemoveInitConsumer> logger,
    IHubContext<RunnerHub> hubContext,
    RunnerSelectionService runnerSelection,
    ParamResolverFactory paramResolverFactory)
    : StateMigrationInitConsumer<RemoveInitRequested, RemoveInitFaulted>(
        logger, hubContext, runnerSelection, paramResolverFactory)
{
    protected override string Endpoint => RunnerEndpoints.RemoveInit;
}
