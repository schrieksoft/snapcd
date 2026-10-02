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
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.Crud.Transfers;

namespace SnapCd.Server.Core.Consumers.Tasks.Transfers;

/// <summary>
/// Pulls this Module's share of the monolith's state and pins it. The source writes the fragment
/// out; the receiver is handed that same fragment and applies it.
/// </summary>
public class TransferMigrateMapConsumer
    : TransferStepConsumer<TransferMigrateMapRequested, TransferMigrateMapFaulted>
{
    public TransferMigrateMapConsumer(
        ILogger<TransferMigrateMapConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection)
        : base(logger, hubContext, runnerSelection) { }

    protected override string Endpoint => nameof(ITransferEndpoints.TransferMigrateMap);

    protected override Task<object> BuildPayload(ConsumeContext<TransferMigrateMapRequested> context, Guid jobId)
    {
        var msg = context.Message;

        return Task.FromResult<object>(new TransferMigrateMapRequestBase
        {
            JobId = jobId,
            OrganizationId = msg.OrganizationId,
            ModuleId = msg.ModuleId,
            Metadata = StepRequestBuilders.MetadataFor(msg.Declared),
            Engine = msg.Declared.Engine,
            RootDirectory = msg.RootDirectory,
            SourceFragment = msg.SourceFragment,
            SourceFragmentMeta = msg.SourceFragmentMeta
        });
    }
}

/// <summary>
/// Reads the committed map to learn which part this root plays, before the map step that cannot be
/// attempted without knowing it.
/// </summary>
public class AnalyseTransferRefactorMapConsumer
    : TransferStepConsumer<AnalyseTransferRefactorMapRequested, AnalyseTransferRefactorMapFaulted>
{
    public AnalyseTransferRefactorMapConsumer(
        ILogger<AnalyseTransferRefactorMapConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection)
        : base(logger, hubContext, runnerSelection) { }

    protected override string Endpoint => nameof(ITransferEndpoints.AnalyseTransferRefactorMap);

    protected override Task<object> BuildPayload(ConsumeContext<AnalyseTransferRefactorMapRequested> context, Guid jobId)
    {
        var msg = context.Message;

        return Task.FromResult<object>(new AnalyseTransferRefactorMapRequestBase
        {
            JobId = jobId,
            OrganizationId = msg.OrganizationId,
            ModuleId = msg.ModuleId,
            Metadata = StepRequestBuilders.MetadataFor(msg.Declared),
            Engine = msg.Declared.Engine,
            RootDirectory = msg.RootDirectory
        });
    }
}

/// <summary>
/// Plans the pinned root and answers whether it comes out with nothing to change, which is what a
/// transfer treats as proof that the move is safe.
/// </summary>
public class TransferMigrateProveConsumer
    : TransferStepConsumer<TransferMigrateProveRequested, TransferMigrateProveFaulted>
{
    public TransferMigrateProveConsumer(
        ILogger<TransferMigrateProveConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection)
        : base(logger, hubContext, runnerSelection) { }

    protected override string Endpoint => nameof(ITransferEndpoints.TransferMigrateProve);

    protected override Task<object> BuildPayload(ConsumeContext<TransferMigrateProveRequested> context, Guid jobId)
    {
        var msg = context.Message;

        return Task.FromResult<object>(new TransferMigrateProveRequestBase
        {
            JobId = jobId,
            OrganizationId = msg.OrganizationId,
            ModuleId = msg.ModuleId,
            Metadata = StepRequestBuilders.MetadataFor(msg.Declared),
            Engine = msg.Declared.Engine,
            RootDirectory = msg.RootDirectory,
            ReceiverOutputs = msg.ReceiverOutputs
        });
    }
}

/// <summary>
/// Writes this Module's share of the move into its own state. The receiver injects, the source
/// strips; demonolith refuses to strip without the receiver's run receipt, which travels here.
/// </summary>
public class TransferMigrateRunConsumer
    : TransferStepConsumer<TransferMigrateRunRequested, TransferMigrateRunFaulted>
{
    public TransferMigrateRunConsumer(
        ILogger<TransferMigrateRunConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection)
        : base(logger, hubContext, runnerSelection) { }

    protected override string Endpoint => nameof(ITransferEndpoints.TransferMigrateRun);

    protected override Task<object> BuildPayload(ConsumeContext<TransferMigrateRunRequested> context, Guid jobId)
    {
        var msg = context.Message;

        return Task.FromResult<object>(new TransferMigrateRunRequestBase
        {
            JobId = jobId,
            OrganizationId = msg.OrganizationId,
            ModuleId = msg.ModuleId,
            Metadata = StepRequestBuilders.MetadataFor(msg.Declared),
            Engine = msg.Declared.Engine,
            RootDirectory = msg.RootDirectory
        });
    }
}

/// <summary>Checks the written state plans clean, which closes this Module's move.</summary>
public class TransferMigrateVerifyConsumer
    : TransferStepConsumer<TransferMigrateVerifyRequested, TransferMigrateVerifyFaulted>
{
    public TransferMigrateVerifyConsumer(
        ILogger<TransferMigrateVerifyConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection)
        : base(logger, hubContext, runnerSelection) { }

    protected override string Endpoint => nameof(ITransferEndpoints.TransferMigrateVerify);

    protected override Task<object> BuildPayload(ConsumeContext<TransferMigrateVerifyRequested> context, Guid jobId)
    {
        var msg = context.Message;

        return Task.FromResult<object>(new TransferMigrateVerifyRequestBase
        {
            JobId = jobId,
            OrganizationId = msg.OrganizationId,
            ModuleId = msg.ModuleId,
            Metadata = StepRequestBuilders.MetadataFor(msg.Declared),
            Engine = msg.Declared.Engine,
            RootDirectory = msg.RootDirectory
        });
    }
}

