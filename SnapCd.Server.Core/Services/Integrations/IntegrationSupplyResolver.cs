// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;

namespace SnapCd.Server.Core.Services.Integrations;

/// <summary>
/// Mirrors <c>AgentSupplyResolver</c>: an integration is "supplied" to a scope when
/// <c>Integration.IsSuppliedToAllModules</c> is true OR a matching per-scope
/// <c>Integration{Scope}Assignment</c> row exists. Phase 4's dispatch uses this for supply ∩ demand.
/// </summary>
public class IntegrationSupplyResolver(IDbContextFactory<SnapCdDbContext> dbContextFactory)
{
    public async Task<bool> IsIntegrationSuppliedToModule(Guid integrationId, Guid moduleId, Guid organizationId)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var moduleInfo = await db.Modules
            .Where(m => m.Id == moduleId && m.OrganizationId == organizationId)
            .Select(m => new { m.Id, m.NamespaceId, StackId = m.Namespace.StackId })
            .FirstOrDefaultAsync();
        if (moduleInfo == null)
            return false;

        return await db.Integrations
            .Where(i => i.Id == integrationId && i.OrganizationId == organizationId)
            .AnyAsync(i =>
                i.IsSuppliedToAllModules ||
                i.ModuleAssignments.Any(x => x.ModuleId == moduleId) ||
                i.NamespaceAssignments.Any(x => x.NamespaceId == moduleInfo.NamespaceId) ||
                i.StackAssignments.Any(x => x.StackId == moduleInfo.StackId));
    }

    public async Task<bool> IsIntegrationSuppliedToNamespace(Guid integrationId, Guid namespaceId, Guid organizationId)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        var nsInfo = await db.Namespaces
            .Where(n => n.Id == namespaceId && n.OrganizationId == organizationId)
            .Select(n => new { n.Id, n.StackId })
            .FirstOrDefaultAsync();
        if (nsInfo == null)
            return false;

        return await db.Integrations
            .Where(i => i.Id == integrationId && i.OrganizationId == organizationId)
            .AnyAsync(i =>
                i.IsSuppliedToAllModules ||
                i.NamespaceAssignments.Any(x => x.NamespaceId == namespaceId) ||
                i.StackAssignments.Any(x => x.StackId == nsInfo.StackId));
    }

    public async Task<bool> IsIntegrationSuppliedToStack(Guid integrationId, Guid stackId, Guid organizationId)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();

        return await db.Integrations
            .Where(i => i.Id == integrationId && i.OrganizationId == organizationId)
            .AnyAsync(i =>
                i.IsSuppliedToAllModules ||
                i.StackAssignments.Any(x => x.StackId == stackId));
    }

    /// <summary>Org-wide demand requires org-wide supply — only the <c>IsSuppliedToAllModules</c> flag
    /// provides it (there is deliberately no org-scope assignment sibling).</summary>
    public async Task<bool> IsIntegrationSuppliedOrgWide(Guid integrationId, Guid organizationId)
    {
        await using var db = await dbContextFactory.CreateDbContextAsync();
        return await db.Integrations
            .Where(i => i.Id == integrationId && i.OrganizationId == organizationId)
            .AnyAsync(i => i.IsSuppliedToAllModules);
    }
}
