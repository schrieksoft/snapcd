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

namespace SnapCd.Server.Core.Services.Integrations;

/// <summary>
/// Resolves which integration-event subscriptions fire for a trigger on a module's scope chain, applying
/// the supply ∩ demand rule: an event matches only if its target integration is also *supplied* to the
/// event's scope. Mirrors <c>MissionMatcher</c>'s gather step; Phase-5 dispatch consumes the result.
/// </summary>
public class IntegrationEventMatcher(IDbContextFactory<SnapCdDbContext> dbFactory, IntegrationSupplyResolver supply)
{
    public readonly record struct IntegrationEventMatch(
        Guid EventId, IntegrationEventScope Scope, Guid IntegrationId, IntegrationTrigger Trigger, string? Template, string? Filter);

    public async Task<List<IntegrationEventMatch>> MatchAsync(
        Guid moduleId, Guid organizationId, IntegrationTrigger trigger, CancellationToken ct = default)
    {
        await using var db = await dbFactory.CreateDbContextAsync(ct);

        var scope = await db.Modules
            .Where(m => m.Id == moduleId && m.OrganizationId == organizationId)
            .Select(m => new { m.NamespaceId, m.Namespace.StackId })
            .FirstOrDefaultAsync(ct);
        if (scope is null) return [];

        var candidates = new List<IntegrationEventMatch>();

        candidates.AddRange(await db.OrganizationIntegrationEvents
            .Where(x => x.OrganizationId == organizationId && !x.IsDisabled && x.Trigger == trigger)
            .Select(x => new IntegrationEventMatch(x.Id, IntegrationEventScope.Organization, x.IntegrationId, x.Trigger, x.Template, x.Filter)).ToListAsync(ct));
        candidates.AddRange(await db.StackIntegrationEvents
            .Where(x => x.OrganizationId == organizationId && x.StackId == scope.StackId && !x.IsDisabled && x.Trigger == trigger)
            .Select(x => new IntegrationEventMatch(x.Id, IntegrationEventScope.Stack, x.IntegrationId, x.Trigger, x.Template, x.Filter)).ToListAsync(ct));
        candidates.AddRange(await db.NamespaceIntegrationEvents
            .Where(x => x.OrganizationId == organizationId && x.NamespaceId == scope.NamespaceId && !x.IsDisabled && x.Trigger == trigger)
            .Select(x => new IntegrationEventMatch(x.Id, IntegrationEventScope.Namespace, x.IntegrationId, x.Trigger, x.Template, x.Filter)).ToListAsync(ct));
        candidates.AddRange(await db.ModuleIntegrationEvents
            .Where(x => x.OrganizationId == organizationId && x.ModuleId == moduleId && !x.IsDisabled && x.Trigger == trigger)
            .Select(x => new IntegrationEventMatch(x.Id, IntegrationEventScope.Module, x.IntegrationId, x.Trigger, x.Template, x.Filter)).ToListAsync(ct));

        var matched = new List<IntegrationEventMatch>();
        foreach (var c in candidates)
        {
            var supplied = c.Scope switch
            {
                IntegrationEventScope.Organization => await supply.IsIntegrationSuppliedOrgWide(c.IntegrationId, organizationId),
                IntegrationEventScope.Stack => await supply.IsIntegrationSuppliedToStack(c.IntegrationId, scope.StackId, organizationId),
                IntegrationEventScope.Namespace => await supply.IsIntegrationSuppliedToNamespace(c.IntegrationId, scope.NamespaceId, organizationId),
                IntegrationEventScope.Module => await supply.IsIntegrationSuppliedToModule(c.IntegrationId, moduleId, organizationId),
                _ => false
            };
            if (supplied) matched.Add(c);
        }

        return matched;
    }
}
