// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.Crud.Transfers;

namespace SnapCd.Server.Core.Consumers.System.Competing;

/// <summary>
/// Closes a transfer once both sides have finished, which releases both Modules. Whether they
/// succeeded is not asked: a further attempt is a new transfer, so what matters is that neither
/// module is still busy under this one.
/// </summary>
public class TransferJobEndedCompetingConsumer(
    IDbContextFactory<SnapCdDbContext> dbContextFactory,
    TransferServiceFactory transferServiceFactory) : IConsumer<ManualJobUpdatedEvent>
{
    public async Task Consume(ConsumeContext<ManualJobUpdatedEvent> context)
    {
        var message = context.Message;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var transferId = await dbContext.ManualModuleJobs.AsNoTracking()
            .Where(j => j.Id == message.JobId && j.OrganizationId == message.OrganizationId)
            .Select(j => j.TransferId)
            .FirstOrDefaultAsync();

        if (transferId is null) return;

        using var transfers = transferServiceFactory.Create();
        await transfers.CloseIfBothSidesEnded(transferId.Value, message.OrganizationId);
    }
}
