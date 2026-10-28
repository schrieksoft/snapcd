// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Services.RippleProbes;

public class RippleProbeConsumerLookupFactory(IDbContextFactory<SnapCdDbContext> dbFactory)
{
    public RippleProbeConsumerLookup Create()
    {
        return new RippleProbeConsumerLookup(dbFactory.CreateDbContext());
    }
}

/// <summary>
/// Given a Module and the outputs its plan says will change, every Module that consumes one of
/// them and which of its own variables that output feeds.
///
/// Three reads rather than one filtered read, because the three input kinds carry different edges.
/// ModuleParamFromOutput and ModuleEnvVarFromOutput name a single output and rename it to a
/// variable. ModuleParamFromOutputSet names the producer alone: every output reaches the consumer
/// under its own name, filtered against the consumer's latest VariableSet.
/// </summary>
public class RippleProbeConsumerLookup : IDisposable
{
    private readonly SnapCdDbContext _dbContext;

    public RippleProbeConsumerLookup(SnapCdDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<RippleProbeConsumer>> ListConsumers(
        Guid outputModuleId,
        Guid organizationId,
        IReadOnlyCollection<string> changedOutputNames)
    {
        if (changedOutputNames.Count == 0) return [];

        var names = changedOutputNames.ToList();
        var byModule = new Dictionary<Guid, RippleProbeConsumer>();

        foreach (var edge in await PerOutputEdges<ModuleParamFromOutput>(outputModuleId, organizationId, names))
            Record(byModule, edge.ModuleId, edge.OutputName, edge.VariableName);

        foreach (var edge in await PerOutputEdges<ModuleEnvVarFromOutput>(outputModuleId, organizationId, names))
            Record(byModule, edge.ModuleId, edge.OutputName, edge.VariableName);

        foreach (var (moduleId, outputName) in await OutputSetEdges(outputModuleId, organizationId, names))
            Record(byModule, moduleId, outputName, outputName);

        return byModule.Values.ToList();
    }

    /// <summary>
    /// An edge naming one output. The same-stack predicate is a filter rather than a refusal: this
    /// answers which Modules are affected, and one in another stack is not.
    /// </summary>
    private async Task<List<(Guid ModuleId, string OutputName, string VariableName)>> PerOutputEdges<TEntity>(
        Guid outputModuleId,
        Guid organizationId,
        List<string> changedOutputNames)
        where TEntity : ModuleInputFromOutput
    {
        var rows = await _dbContext.Set<TEntity>()
            .Where(x =>
                x.OutputModuleId == outputModuleId &&
                x.OrganizationId == organizationId &&
                x.Module.Namespace.StackId == x.OutputModule.Namespace.StackId &&
                changedOutputNames.Contains(x.OutputName))
            .Select(x => new { x.ModuleId, x.OutputName, VariableName = x.Name })
            .ToListAsync();

        return rows.Select(r => (r.ModuleId, r.OutputName, r.VariableName)).ToList();
    }

    /// <summary>
    /// An edge naming the producer alone. Every changed output reaches the consumer under its own
    /// name, kept only where the consumer's latest VariableSet declares it. A consumer with no
    /// VariableSet at all takes every changed output, which is what the trigger query does and what
    /// OutputSetParamResolver's filter amounts to when there is nothing to filter against.
    /// </summary>
    private async Task<List<(Guid ModuleId, string OutputName)>> OutputSetEdges(
        Guid outputModuleId,
        Guid organizationId,
        List<string> changedOutputNames)
    {
        var moduleIds = await _dbContext.ModuleParamFromOutputSets
            .Where(x =>
                x.OutputModuleId == outputModuleId &&
                x.OrganizationId == organizationId &&
                x.Module.Namespace.StackId == x.OutputModule.Namespace.StackId)
            .Select(x => x.ModuleId)
            .Distinct()
            .ToListAsync();

        var edges = new List<(Guid, string)>();

        foreach (var moduleId in moduleIds)
        {
            // The latest set only. Taking every set would union names a later one dropped, so a
            // variable removed from the Module would still look declared.
            var latest = await _dbContext.VariableSets
                .Where(vs => vs.ModuleId == moduleId && vs.OrganizationId == organizationId)
                .OrderByDescending(vs => vs.Timestamp)
                .Include(vs => vs.Variables)
                .FirstOrDefaultAsync();

            var reaching = latest is null
                ? changedOutputNames
                : changedOutputNames.Where(n => latest.Variables.Any(v => v.Name == n)).ToList();

            foreach (var name in reaching)
                edges.Add((moduleId, name));
        }

        return edges;
    }

    private static void Record(
        Dictionary<Guid, RippleProbeConsumer> byModule,
        Guid moduleId,
        string outputName,
        string variableName)
    {
        if (!byModule.TryGetValue(moduleId, out var consumer))
        {
            consumer = new RippleProbeConsumer { ModuleId = moduleId };
            byModule[moduleId] = consumer;
        }

        consumer.VariableNamesByOutputName[outputName] = variableName;
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
