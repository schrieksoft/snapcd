// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Steps.StateMigrations;

namespace SnapCd.Server.Core.Services.Crud.StateMigrations;

/// <summary>
/// The per-address record of what a job did. Every job that moves or observes state writes these,
/// so "what happened to this resource" has one answer whichever job type touched it.
/// </summary>
public class ManualJobAddressService(IDbContextFactory<SnapCdDbContext> dbContextFactory)
{
    public async Task Record(
        Guid jobId,
        Guid organizationId,
        Guid moduleId,
        AddressOperation operation,
        IReadOnlyCollection<AddressResult> results)
    {
        if (results.Count == 0) return;

        await using var dbContext = await dbContextFactory.CreateDbContextAsync();

        var now = DateTimeOffset.UtcNow;

        foreach (var result in results)
            dbContext.ManualModuleJobAddresses.Add(new ManualModuleJobAddress
            {
                Id = Guid.NewGuid(),
                OrganizationId = organizationId,
                JobId = jobId,
                ModuleId = moduleId,
                Address = result.Address,
                Target = result.Target,
                Operation = operation,
                Outcome = result.Outcome,
                RecordedAt = now
            });

        await dbContext.SaveChangesAsync();
    }
}
