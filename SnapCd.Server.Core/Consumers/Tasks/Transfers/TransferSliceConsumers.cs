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

    protected override string Endpoint => RunnerEndpoints.TransferMigrateMap;

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

    protected override string Endpoint => RunnerEndpoints.TransferMigrateProve;

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
            RootDirectory = msg.RootDirectory
        });
    }
}

/// <summary>
/// Checks the committed code against the map, so a Module whose code has moved on since the map
/// was made is caught before its state is touched.
/// </summary>
public class TransferRefactorDiffConsumer
    : TransferStepConsumer<TransferRefactorDiffRequested, TransferRefactorDiffFaulted>
{
    public TransferRefactorDiffConsumer(
        ILogger<TransferRefactorDiffConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection)
        : base(logger, hubContext, runnerSelection) { }

    protected override string Endpoint => RunnerEndpoints.TransferRefactorDiff;

    protected override Task<object> BuildPayload(ConsumeContext<TransferRefactorDiffRequested> context, Guid jobId)
    {
        var msg = context.Message;

        return Task.FromResult<object>(new TransferRefactorDiffRequestBase
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

    protected override string Endpoint => RunnerEndpoints.TransferMigrateRun;

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

    protected override string Endpoint => RunnerEndpoints.TransferMigrateVerify;

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

/// <summary>
/// Reads this Module's outputs after the write, so the other side of the transfer can plan against
/// values that exist.
/// </summary>
public class TransferOutputsConsumer
    : TransferStepConsumer<TransferOutputsRequested, TransferOutputsFaulted>
{
    public TransferOutputsConsumer(
        ILogger<TransferOutputsConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection)
        : base(logger, hubContext, runnerSelection) { }

    protected override string Endpoint => RunnerEndpoints.TransferOutputs;

    protected override Task<object> BuildPayload(ConsumeContext<TransferOutputsRequested> context, Guid jobId)
    {
        var msg = context.Message;

        return Task.FromResult<object>(new TransferOutputsRequestBase
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
