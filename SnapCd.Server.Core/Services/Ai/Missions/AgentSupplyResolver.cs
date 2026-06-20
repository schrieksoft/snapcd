// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;

namespace SnapCd.Server.Core.Services.Ai.Missions;

/// <summary>
/// Mirrors <c>ServicePrincipalRepository.CanRunModule()</c> shape for Agent supply checks.
/// An Agent is "supplied" to a scope when <c>Agent.IsSuppliedToAllModules</c> is true OR a matching
/// per-scope <c>Agent{Scope}Assignment</c> row exists. Used by the Mission-create gate to enforce
/// the supply/demand duality on top of the existing scope-role authorization.
/// </summary>
public class AgentSupplyResolver
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;

    public AgentSupplyResolver(IDbContextFactory<SnapCdDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<bool> IsAgentSuppliedToModule(Guid agentId, Guid moduleId, Guid organizationId)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var moduleInfo = await db.Modules
            .Where(m => m.Id == moduleId && m.OrganizationId == organizationId)
            .Select(m => new { m.Id, m.NamespaceId, StackId = m.Namespace.StackId })
            .FirstOrDefaultAsync();
        if (moduleInfo == null)
            return false;

        return await db.Agents
            .Where(a => a.Id == agentId && a.OrganizationId == organizationId)
            .AnyAsync(a =>
                a.IsSuppliedToAllModules ||
                a.AgentModuleSupplies.Any(x => x.ModuleId == moduleId) ||
                a.AgentNamespaceSupplies.Any(x => x.NamespaceId == moduleInfo.NamespaceId) ||
                a.AgentStackSupplies.Any(x => x.StackId == moduleInfo.StackId));
    }

    public async Task<bool> IsAgentSuppliedToNamespace(Guid agentId, Guid namespaceId, Guid organizationId)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        var nsInfo = await db.Namespaces
            .Where(n => n.Id == namespaceId && n.OrganizationId == organizationId)
            .Select(n => new { n.Id, n.StackId })
            .FirstOrDefaultAsync();
        if (nsInfo == null)
            return false;

        return await db.Agents
            .Where(a => a.Id == agentId && a.OrganizationId == organizationId)
            .AnyAsync(a =>
                a.IsSuppliedToAllModules ||
                a.AgentNamespaceSupplies.Any(x => x.NamespaceId == namespaceId) ||
                a.AgentStackSupplies.Any(x => x.StackId == nsInfo.StackId));
    }

    public async Task<bool> IsAgentSuppliedToStack(Guid agentId, Guid stackId, Guid organizationId)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();

        return await db.Agents
            .Where(a => a.Id == agentId && a.OrganizationId == organizationId)
            .AnyAsync(a =>
                a.IsSuppliedToAllModules ||
                a.AgentStackSupplies.Any(x => x.StackId == stackId));
    }

    /// <summary>
    /// Org-wide demand (an <c>OrganizationMission</c> fires across every module in the org) requires
    /// org-wide supply. The only mechanism for that is the <c>IsSuppliedToAllModules</c> flag — there is
    /// deliberately no <c>AgentOrganizationSupply</c> sibling (mirrors Runner: no equivalent there).
    /// </summary>
    public async Task<bool> IsAgentSuppliedOrgWide(Guid agentId, Guid organizationId)
    {
        await using var db = await _dbContextFactory.CreateDbContextAsync();
        return await db.Agents
            .Where(a => a.Id == agentId && a.OrganizationId == organizationId)
            .AnyAsync(a => a.IsSuppliedToAllModules);
    }
}
