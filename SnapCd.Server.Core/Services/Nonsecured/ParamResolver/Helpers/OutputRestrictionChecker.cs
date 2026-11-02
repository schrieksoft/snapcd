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
/// Decides which of a producing Module's Outputs a consuming Module may read. A Module restricts its whole
/// Output Set through <see cref="Module.OutputSetDownstreamRestriction"/>, or individual Outputs by name
/// through <see cref="ModuleOutputDownstreamRestriction"/>.
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
        Guid organizationId)
    {
        if (outputs.Count == 0) return outputs;

        var producerByOutputSet = await ProducerByOutputSet(outputs, organizationId);
        if (producerByOutputSet.Count == 0) return outputs;

        var namedRestrictions = await NamedRestrictions(producerByOutputSet.Values.Select(p => p.Id), organizationId);

        return outputs.Where(o =>
        {
            if (!producerByOutputSet.TryGetValue(o.OutputSetId, out var producer)) return true;
            return !IsWithheld(producer, o, namedRestrictions);
        }).ToList();
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

        var producers = await _dbContext.Modules
            .Where(m => producerIds.Contains(m.Id) && m.OrganizationId == organizationId)
            .Select(m => new ProducerRestriction(m.Id, m.Name, m.OutputSetDownstreamRestriction))
            .ToListAsync();

        var namedRestrictions = await NamedRestrictions(producerIds, organizationId);

        var sensitiveNames = await SensitiveOutputNames(producerIds, organizationId);

        var refusals = new List<string>();

        foreach (var (producerModuleId, outputName) in requested)
        {
            var producer = producers.FirstOrDefault(p => p.Id == producerModuleId);
            if (producer == null) continue;

            var isSensitive = sensitiveNames.TryGetValue(producerModuleId, out var secrets) && secrets.Contains(outputName);
            var reason = WithholdingReason(producer, outputName, isSensitive, namedRestrictions);

            if (reason != null)
                refusals.Add($"\"{outputName}\" from Module \"{producer.Name}\", which {reason}");
        }

        if (refusals.Count == 0) return;

        var consumerName = await _dbContext.Modules
            .Where(m => m.Id == consumingModuleId)
            .Select(m => m.Name)
            .FirstOrDefaultAsync() ?? consumingModuleId.ToString();

        throw new OutputRestrictedException(
            $"Module \"{consumerName}\" cannot read {string.Join("; ", refusals)}. " +
            "Supply the Output to this Module or its Namespace to allow it.");
    }

    private async Task<Dictionary<Guid, ProducerRestriction>> ProducerByOutputSet(List<Output> outputs, Guid organizationId)
    {
        var outputSetIds = outputs.Select(o => o.OutputSetId).Distinct().ToList();

        var rows = await _dbContext.OutputSets
            .Where(os => outputSetIds.Contains(os.Id) && os.OrganizationId == organizationId)
            .Select(os => new
            {
                os.Id,
                Producer = new ProducerRestriction(os.Module.Id, os.Module.Name, os.Module.OutputSetDownstreamRestriction)
            })
            .ToListAsync();

        return rows.ToDictionary(r => r.Id, r => r.Producer);
    }

    private async Task<Dictionary<Guid, HashSet<string>>> NamedRestrictions(IEnumerable<Guid> producerIds, Guid organizationId)
    {
        var ids = producerIds.Distinct().ToList();

        var rows = await _dbContext.ModuleOutputDownstreamRestrictions
            .Where(r => ids.Contains(r.ModuleId) && r.OrganizationId == organizationId)
            .Select(r => new { r.ModuleId, r.OutputName })
            .ToListAsync();

        return rows
            .GroupBy(r => r.ModuleId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.OutputName).ToHashSet());
    }

    private async Task<Dictionary<Guid, HashSet<string>>> SensitiveOutputNames(List<Guid> producerIds, Guid organizationId)
    {
        var rows = await _dbContext.Set<SecretOutput>()
            .Where(o => o.OrganizationId == organizationId && producerIds.Contains(o.OutputSet.ModuleId))
            .Select(o => new { o.OutputSet.ModuleId, o.Name })
            .ToListAsync();

        return rows
            .GroupBy(r => r.ModuleId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.Name).ToHashSet());
    }

    private static bool IsWithheld(ProducerRestriction producer, Output output, Dictionary<Guid, HashSet<string>> namedRestrictions)
        => WithholdingReason(producer, output.Name, output is SecretOutput, namedRestrictions) != null;

    private static string? WithholdingReason(
        ProducerRestriction producer,
        string outputName,
        bool isSensitive,
        Dictionary<Guid, HashSet<string>> namedRestrictions)
    {
        if (producer.Restriction == OutputSetDownstreamRestriction.All)
            return "withholds its whole Output Set";

        if (producer.Restriction == OutputSetDownstreamRestriction.SensitiveOnly && isSensitive)
            return "withholds its sensitive Outputs";

        if (namedRestrictions.TryGetValue(producer.Id, out var names) && names.Contains(outputName))
            return "withholds that Output";

        return null;
    }

    private record ProducerRestriction(Guid Id, string Name, OutputSetDownstreamRestriction? Restriction);
}
