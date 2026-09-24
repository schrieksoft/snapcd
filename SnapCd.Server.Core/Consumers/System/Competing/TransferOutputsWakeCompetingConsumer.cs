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
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.StateMachine.Transfers.Migrate;

namespace SnapCd.Server.Core.Consumers.System.Competing;

/// <summary>
/// Wakes transfer jobs parked on a Module's outputs when that Module publishes a new output set.
/// The job re-reads them itself; this only says that the answer may have changed.
/// </summary>
public class TransferOutputsWakeCompetingConsumer(
    IDbContextFactory<SnapCdDbContext> dbContextFactory,
    IBus bus) : IConsumer<OutputSetWithOutputsCreatedEvent>
{
    public async Task Consume(ConsumeContext<OutputSetWithOutputsCreatedEvent> context)
    {
        var producerId = context.Message.Data.ModuleId;
        var organizationId = context.Message.OrganizationId;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        // Jobs parked on this Module's outputs, whichever transfer they belong to.
        var parked = await dbContext.Set<TransferMigrateSaga>().AsNoTracking()
            .Where(s => s.OrganizationId == organizationId
                        && s.CurrentState == nameof(TransferMigrateStateMachine.WaitingForOutputs)
                        && s.CounterpartyModuleId == producerId)
            .Select(s => s.CorrelationId)
            .ToListAsync();

        foreach (var jobId in parked)
            await bus.Publish(new OutputsReevaluationRequestedEvent
            {
                ModuleJobId = jobId,
                OrganizationId = organizationId
            });
    }
}
