// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Outputs;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Consumers.System.Competing;

/// <summary>
/// Keeps a Module's OutputSet count bounded, by removing the oldest beyond the configured limit
/// once a new one has been created.
///
/// Nothing holds a reference to an OutputSet row: its Outputs cascade from it, and downstream
/// wiring points at a Module and an output name rather than a set, resolving against whichever set
/// is latest. So the oldest can go without reading anything else.
///
/// The repository raises the DeletedEvent that reclaims the set's sensitive values from the
/// vault, so the delete goes through it rather than removing the rows.
/// </summary>
public class OutputSetCullConsumer(
    SnapCdDbContext dbContext,
    OutputSetRepository repository,
    IOptions<SetRetentionSettings> settings,
    ILogger<OutputSetCullConsumer> logger)
    : IConsumer<OutputSetWithOutputsCreatedEvent>
{
    public async Task Consume(ConsumeContext<OutputSetWithOutputsCreatedEvent> context)
    {
        var retention = settings.Value.OutputSets;
        if (!retention.CullAboveLimit || retention.MaxKeptPerModule < 1) return;

        var moduleId = context.Message.Data.ModuleId;
        var organizationId = context.Message.OrganizationId;

        var doomed = await dbContext.OutputSets
            .Where(s => s.ModuleId == moduleId && s.OrganizationId == organizationId)
            .OrderByDescending(s => s.Timestamp)
            .Skip(retention.MaxKeptPerModule)
            .Select(s => s.Id)
            .ToListAsync(context.CancellationToken);

        if (doomed.Count == 0) return;

        var removed = 0;
        foreach (var id in doomed)
        {
            try
            {
                await repository.Delete(id, organizationId);
                removed++;
            }
            catch (Exception ex)
            {
                // One set failing says nothing about the next, and the Module's next apply tries again.
                logger.LogWarning(ex, "Could not cull OutputSet {OutputSetId}", id);
            }
        }

        logger.LogDebug(
            "Culled {Removed} of {Found} OutputSet(s) for Module {ModuleId}", removed, doomed.Count, moduleId);
    }
}
