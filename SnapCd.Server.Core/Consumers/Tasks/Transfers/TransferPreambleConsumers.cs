// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Endpoints;
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
        RunnerSelectionService runnerSelection)
        : base(logger, hubContext, runnerSelection) { }

    protected override string Endpoint => nameof(ITransferEndpoints.TransferGetModule);

    protected override Task<object> BuildPayload(ConsumeContext<TransferGetModuleRequested> context, Guid jobId)
    {
        var msg = context.Message;
        var ordinary = StepRequestBuilders.GetModule(jobId, msg.OrganizationId, msg.Declared);

        // The transfer runs against the ref carrying the code move, not the Module's own.
        if (!string.IsNullOrWhiteSpace(msg.SourceRevisionOverride))
            ordinary.SourceRevision = msg.SourceRevisionOverride;

        return Task.FromResult<object>(ordinary);
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
        ParamResolverFactory paramResolverFactory)
        : base(logger, hubContext, runnerSelection)
    {
        _paramResolverFactory = paramResolverFactory;
    }

    protected override string Endpoint => nameof(ITransferEndpoints.TransferInit);

    protected override async Task<object> BuildPayload(ConsumeContext<TransferInitRequested> context, Guid jobId)
    {
        var msg = context.Message;

        var resolvedEnvVars = await StepRequestBuilders.ResolveInitEnvVars(
            _paramResolverFactory, jobId, msg.OrganizationId, msg.Declared, _logger);

        var ordinary = StepRequestBuilders.Init(jobId, msg.OrganizationId, msg.Declared, resolvedEnvVars);

        return ordinary;
    }
}

public class TransferValidateConsumer
    : TransferStepConsumer<TransferValidateRequested, TransferValidateFaulted>
{
    public TransferValidateConsumer(
        ILogger<TransferValidateConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection)
        : base(logger, hubContext, runnerSelection) { }

    protected override string Endpoint => nameof(ITransferEndpoints.TransferValidate);

    protected override Task<object> BuildPayload(ConsumeContext<TransferValidateRequested> context, Guid jobId)
    {
        var msg = context.Message;
        var ordinary = StepRequestBuilders.Validate(jobId, msg.OrganizationId, msg.Declared);

        return Task.FromResult<object>(ordinary);
    }
}
