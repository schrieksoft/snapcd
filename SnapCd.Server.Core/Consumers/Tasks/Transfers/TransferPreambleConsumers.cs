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
using SnapCd.Contracts.RunnerRequests.Transfers;
using SnapCd.Server.Core.Consumers.Tasks.Builders;
using SnapCd.Server.Core.Events.Steps.Transfer;
using SnapCd.Server.Core.Factories;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.Crud.Transfers;

namespace SnapCd.Server.Core.Consumers.Tasks.Transfers;

/// <summary>
/// Checks the Module out at the ref its side of the transfer is being proved against, which is the
/// consented ref rather than the Module's own.
/// </summary>
public class TransferGetModuleConsumer
    : TransferStepConsumer<TransferGetModuleRequested, TransferGetModuleFaulted>
{
    public TransferGetModuleConsumer(
        ILogger<TransferGetModuleConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection,
        TransferDispatchGate dispatchGate)
        : base(logger, hubContext, runnerSelection, dispatchGate) { }

    protected override string Endpoint => RunnerEndpoints.TransferGetModule;

    protected override Task<object> BuildPayload(ConsumeContext<TransferGetModuleRequested> context, Guid jobId)
    {
        var msg = context.Message;
        var ordinary = StepRequestBuilders.GetModule(jobId, msg.OrganizationId, msg.Declared);

        return Task.FromResult<object>(new TransferGetModuleRequestBase
        {
            JobId = ordinary.JobId,
            OrganizationId = ordinary.OrganizationId,
            ModuleId = msg.ModuleId,
            Metadata = ordinary.Metadata,
            SourceType = ordinary.SourceType,
            SourceRevisionType = ordinary.SourceRevisionType,
            SourceUrl = ordinary.SourceUrl,
            SourceRevision = string.IsNullOrWhiteSpace(msg.SourceRevisionOverride)
                ? ordinary.SourceRevision
                : msg.SourceRevisionOverride,
            Engine = ordinary.Engine,
            CleanInitEnabled = ordinary.CleanInitEnabled,
            ExtraFiles = ordinary.ExtraFiles
        });
    }
}

public class TransferInitConsumer
    : TransferStepConsumer<TransferInitRequested, TransferInitFaulted>
{
    private readonly ParamResolverFactory _paramResolverFactory;

    public TransferInitConsumer(
        ILogger<TransferInitConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection,
        TransferDispatchGate dispatchGate,
        ParamResolverFactory paramResolverFactory)
        : base(logger, hubContext, runnerSelection, dispatchGate)
    {
        _paramResolverFactory = paramResolverFactory;
    }

    protected override string Endpoint => RunnerEndpoints.TransferInit;

    protected override async Task<object> BuildPayload(ConsumeContext<TransferInitRequested> context, Guid jobId)
    {
        var msg = context.Message;

        var resolvedEnvVars = await StepRequestBuilders.ResolveInitEnvVars(
            _paramResolverFactory, jobId, msg.OrganizationId, msg.Declared, _logger);

        var ordinary = StepRequestBuilders.Init(jobId, msg.OrganizationId, msg.Declared, resolvedEnvVars);

        return new TransferInitRequestBase
        {
            JobId = ordinary.JobId,
            OrganizationId = ordinary.OrganizationId,
            ModuleId = msg.ModuleId,
            Metadata = ordinary.Metadata,
            Engine = ordinary.Engine,
            InitBeforeHook = ordinary.InitBeforeHook,
            InitAfterHook = ordinary.InitAfterHook,
            CleanInitEnabled = ordinary.CleanInitEnabled,
            ResolvedEnvVars = ordinary.ResolvedEnvVars,
            BackendConfiguration = ordinary.BackendConfiguration
        };
    }
}

public class TransferValidateConsumer
    : TransferStepConsumer<TransferValidateRequested, TransferValidateFaulted>
{
    public TransferValidateConsumer(
        ILogger<TransferValidateConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection,
        TransferDispatchGate dispatchGate)
        : base(logger, hubContext, runnerSelection, dispatchGate) { }

    protected override string Endpoint => RunnerEndpoints.TransferValidate;

    protected override Task<object> BuildPayload(ConsumeContext<TransferValidateRequested> context, Guid jobId)
    {
        var msg = context.Message;
        var ordinary = StepRequestBuilders.Validate(jobId, msg.OrganizationId, msg.Declared);

        return Task.FromResult<object>(new TransferValidateRequestBase
        {
            JobId = ordinary.JobId,
            OrganizationId = ordinary.OrganizationId,
            ModuleId = msg.ModuleId,
            Metadata = ordinary.Metadata,
            Engine = ordinary.Engine
        });
    }
}

/// <summary>
/// Plans the Module as it stands. A transfer only proceeds from a plan with nothing to change, so
/// this is what establishes that the Module is settled before its state is touched.
/// </summary>
public class TransferPlanConsumer
    : TransferStepConsumer<TransferPlanRequested, TransferPlanFaulted>
{
    private readonly ParamResolverFactory _paramResolverFactory;

    public TransferPlanConsumer(
        ILogger<TransferPlanConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection,
        TransferDispatchGate dispatchGate,
        ParamResolverFactory paramResolverFactory)
        : base(logger, hubContext, runnerSelection, dispatchGate)
    {
        _paramResolverFactory = paramResolverFactory;
    }

    protected override string Endpoint => RunnerEndpoints.TransferPlan;

    protected override async Task<object> BuildPayload(ConsumeContext<TransferPlanRequested> context, Guid jobId)
    {
        var msg = context.Message;

        var resolvedParameters = await StepRequestBuilders.ResolvePlanParameters(
            _paramResolverFactory, jobId, msg.OrganizationId, msg.Declared, _logger);

        var ordinary = StepRequestBuilders.Plan(
            jobId, msg.OrganizationId, msg.Declared, resolvedParameters, isDestroyJob: false);

        return new TransferPlanRequestBase
        {
            JobId = ordinary.JobId,
            OrganizationId = ordinary.OrganizationId,
            ModuleId = msg.ModuleId,
            Metadata = ordinary.Metadata,
            Engine = ordinary.Engine,
            PlanBeforeHook = ordinary.PlanBeforeHook,
            PlanAfterHook = ordinary.PlanAfterHook,
            ResolvedParameters = ordinary.ResolvedParameters,
            PulumiFlags = ordinary.PulumiFlags,
            PulumiArrayFlags = ordinary.PulumiArrayFlags,
            TerraformFlags = ordinary.TerraformFlags,
            TerraformArrayFlags = ordinary.TerraformArrayFlags,
            Policies = ordinary.Policies
        };
    }
}
