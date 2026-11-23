// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.Outputs;
using SnapCd.Server.Core.Misc.Exceptions;

namespace SnapCd.Server.Core.Services.Nonsecured.ParamResolver.Helpers;

/// <summary>
/// Decides which of a producing Module's Outputs a consuming Module may read. Both entry points ask
/// <see cref="OutputReadability"/>, so a restriction, an exemption and a supply are weighed in one
/// query and the two cannot disagree.
/// </summary>
public class OutputRestrictionChecker
{
    private readonly SnapCdDbContext _dbContext;

    public OutputRestrictionChecker(SnapCdDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Drops the Outputs of <paramref name="outputs"/> that their producer withholds from the consuming
    /// Module, and returns the rest. Runs before secret values are fetched, so a withheld SecretOutput is
    /// never read from the vault.
    /// </summary>
    public async Task<List<Output>> FilterReadable(
        List<Output> outputs,
        Guid consumingModuleId,
        Guid organizationId)
    {
        if (outputs.Count == 0) return outputs;

        var ids = outputs.Select(o => o.Id).ToList();

        var readable = (await OutputReadability
            .Readable(_dbContext, consumingModuleId, organizationId)
            .Where(o => ids.Contains(o.Id))
            .Select(o => o.Id)
            .ToListAsync())
            .ToHashSet();

        return outputs.Where(o => readable.Contains(o.Id)).ToList();
    }

    /// <summary>
    /// Throws when the consuming Module names an Output its producer withholds. Used where the input names
    /// a specific Output, so silently dropping it would surface later as a missing variable.
    /// </summary>
    public async Task EnsureReadable(
        Guid consumingModuleId,
        IReadOnlyCollection<(Guid ProducerModuleId, string OutputName)> requested,
        Guid organizationId)
    {
        if (requested.Count == 0) return;

        var producerIds = requested.Select(r => r.ProducerModuleId).Distinct().ToList();

        // Readable, and every Output that exists at all: an Output nobody has produced is not
        // withheld, it is simply absent, and the caller finds that out as a missing variable.
        var rows = await _dbContext.Set<Output>()
            .Where(o => o.OrganizationId == organizationId && producerIds.Contains(o.OutputSet.ModuleId))
            .Select(o => new { ProducerId = o.OutputSet.ModuleId, o.Name })
            .ToListAsync();

        var exists = rows.Select(o => (o.ProducerId, o.Name)).ToHashSet();

        var readable = (await OutputReadability
            .Readable(_dbContext, consumingModuleId, organizationId)
            .Where(o => producerIds.Contains(o.OutputSet.ModuleId))
            .Select(o => new { ProducerId = o.OutputSet.ModuleId, o.Name })
            .ToListAsync())
            .Select(o => (o.ProducerId, o.Name))
            .ToHashSet();

        var refused = requested
            .Where(r => exists.Contains((r.ProducerModuleId, r.OutputName))
                        && !readable.Contains((r.ProducerModuleId, r.OutputName)))
            .ToList();

        if (refused.Count == 0) return;

        var refusedIds = refused.Select(r => r.ProducerModuleId).Distinct().ToList();

        // Carries the effective rule as well as the name, so the message can say why without
        // asking again per refusal.
        var producers = await _dbContext.Modules
            .Where(m => refusedIds.Contains(m.Id) && m.OrganizationId == organizationId)
            .Select(m => new
            {
                m.Id,
                m.Name,
                NamespaceName = m.Namespace.Name,
                Restriction = m.Resolved!.OutputSetDownstreamRestriction
            })
            .ToListAsync();

        var sensitive = (await _dbContext.Set<SecretOutput>()
            .Where(o => o.OrganizationId == organizationId && refusedIds.Contains(o.OutputSet.ModuleId))
            .Select(o => new { ProducerId = o.OutputSet.ModuleId, o.Name })
            .ToListAsync())
            .Select(o => (o.ProducerId, o.Name))
            .ToHashSet();

        var consumerName = await _dbContext.Modules
            .Where(m => m.Id == consumingModuleId)
            .Select(m => m.Name)
            .FirstOrDefaultAsync() ?? consumingModuleId.ToString();

        var refusals = refused.Select(r =>
        {
            var producer = producers.FirstOrDefault(p => p.Id == r.ProducerModuleId);

            if (producer == null) return $"- \"{r.OutputName}\" from Module {r.ProducerModuleId}.";

            var reason = producer.Restriction switch
            {
                OutputSetDownstreamRestriction.All => "withholds all its Outputs",
                OutputSetDownstreamRestriction.SensitiveOnly
                    when sensitive.Contains((r.ProducerModuleId, r.OutputName)) => "withholds its sensitive Outputs",
                _ => "withholds that Output"
            };

            return $"- \"{r.OutputName}\" from Module \"{producer.Name}\" in Namespace " +
                   $"\"{producer.NamespaceName}\", which {reason}.";
        });

        throw new OutputRestrictedException(
            $"Module \"{consumerName}\" cannot read:\n{string.Join("\n", refusals)}\n" +
            "Supply the mentioned Outputs (or full OutputSets) to this Module or its Namespace to allow it.");
    }
}
