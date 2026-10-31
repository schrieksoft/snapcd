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
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.Nonsecured.Vaults;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Consumers.System.Competing;

/// <summary>
/// Keeps an origin Module's probe wave count bounded, by removing the oldest beyond the configured
/// limit once a new one has started.
///
/// The wave is what gets counted, not the sets produced in it. A set is one Module's answer within
/// one wave, read through the attempt that recorded it, where the absence of an answer is what
/// tells a wave it is still waiting; so removing a set from a live wave would leave that wave
/// waiting on a Module that had already replied. Deleting the wave takes its attempts, edges and
/// both kinds of set with it in one statement.
///
/// A wave belongs to the job whose approval it informs, and its origin Module is an attribute of
/// that job, so the grouping goes through the job rather than reading a column off the wave.
///
/// The sets go by SQL cascade, which raises no event, so the sensitive signals' values are swept
/// from the vault here: the rows would otherwise be gone with the values left behind.
/// </summary>
public class ProbeWaveCullConsumer(
    SnapCdDbContext dbContext,
    OrphanedVaultValueSweeper sweeper,
    IOptions<SetRetentionSettings> settings,
    ILogger<ProbeWaveCullConsumer> logger)
    : IConsumer<ProbeWaveCreatedEvent>
{
    public async Task Consume(ConsumeContext<ProbeWaveCreatedEvent> context)
    {
        var retention = settings.Value.ProbeWaves;
        if (!retention.CullAboveLimit || retention.MaxKeptPerModule < 1) return;

        var originModuleId = context.Message.OriginModuleId;
        var organizationId = context.Message.OrganizationId;

        var doomed = await dbContext.ProbeWaves
            .Where(w => w.OrganizationId == organizationId &&
                        w.OriginModuleJob.ModuleId == originModuleId)
            .OrderByDescending(w => w.CreatedDateTime)
            .Skip(retention.MaxKeptPerModule)
            .Select(w => w.Id)
            .ToListAsync(context.CancellationToken);

        if (doomed.Count == 0) return;

        // Which Modules answered these waves, read before the rows go: a sweep decides what to keep
        // from the sets that survive, so it has nothing to work from once they are deleted.
        var answeringModuleIds = await dbContext.SignalOutputSets
            .Where(s => s.OrganizationId == organizationId && doomed.Contains(s.ProbeWaveId))
            .Select(s => s.ModuleId)
            .Distinct()
            .ToListAsync(context.CancellationToken);

        var removed = await dbContext.ProbeWaves
            .Where(w => w.OrganizationId == organizationId && doomed.Contains(w.Id))
            .ExecuteDeleteAsync(context.CancellationToken);

        foreach (var moduleId in answeringModuleIds)
        {
            try
            {
                await sweeper.SweepUnreferencedSignals(
                    moduleId, organizationId, context.CancellationToken);
            }
            catch (Exception ex)
            {
                // One Module's sweep failing says nothing about the next, and the scheduled sweep
                // catches what is left.
                logger.LogWarning(
                    ex, "Could not reclaim signal values for Module {ModuleId}", moduleId);
            }
        }

        logger.LogDebug(
            "Culled {Removed} probe wave(s) for origin Module {ModuleId}", removed, originModuleId);
    }
}
