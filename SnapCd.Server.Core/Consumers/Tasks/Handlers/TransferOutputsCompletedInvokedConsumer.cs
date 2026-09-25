// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Events.Handlers;
using SnapCd.Server.Core.Events.Steps.Transfer;
using SnapCd.Server.Core.Services.Crud;

namespace SnapCd.Server.Core.Consumers.Tasks.Handlers;

/// <summary>
/// Stores what a transferring Module's state now produces, then tells its saga the step is done.
/// The storing happens here rather than in the saga because it opens a transaction of its own,
/// and the saga's context is already in one.
/// </summary>
public class TransferOutputsCompletedInvokedConsumer(
    ILogger<TransferOutputsCompletedInvokedConsumer> logger,
    OutputSetService outputSetService,
    IBus bus) : IConsumer<TransferOutputsCompletedInvoked>
{
    public async Task Consume(ConsumeContext<TransferOutputsCompletedInvoked> context)
    {
        var msg = context.Message;

        if (msg.OutputSet is { } outputSet)
        {
            await outputSetService.CreateWithOutputsNonsecured(
                outputSet, msg.ModuleId, msg.OrganizationId);

            logger.LogDebug("Stored outputs for Module {ModuleId} of job {JobId}",
                msg.ModuleId, msg.JobId);
        }

        await bus.Publish(new TransferOutputsCompleted
        {
            CorrelationId = msg.JobId,
            OrganizationId = msg.OrganizationId,
            ModuleId = msg.ModuleId
        });
    }
}
