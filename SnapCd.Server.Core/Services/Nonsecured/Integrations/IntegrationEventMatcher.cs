// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Services.Utils;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;

namespace SnapCd.Server.Core.Services.Nonsecured.Integrations;

/// <summary>
/// Resolves which integration-event subscriptions fire for a trigger on a module's scope chain, applying
/// the supply ∩ demand rule: an event matches only if its target integration is also *supplied* to the
/// event's scope. Mirrors <c>MissionMatcher</c>'s gather step; Phase-5 dispatch consumes the result.
/// </summary>
public class IntegrationEventMatcherFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory)
{
    public IntegrationEventMatcher Create()
    {
        var dbContext = dbFactory.CreateDbContext();
        return new IntegrationEventMatcher(dbContext, new IntegrationSupplyResolver(dbContext));
    }
}

public class IntegrationEventMatcher(SnapCdDbContext dbContext, IntegrationSupplyResolver supply) : IDisposable
{
    public void Dispose() => dbContext.Dispose();

    /// <param name="Distance">
    /// Hops from the Module to the Namespace that declared the event, so a nearer Namespace
    /// overrides the ones above it. Zero where the scope is not a Namespace, which Scope already
    /// orders.
    /// </param>
    public readonly record struct IntegrationEventMatch(
        Guid EventId, IntegrationEventScope Scope, Guid IntegrationId, IntegrationTrigger Trigger, string? Template, string? Filter, int Distance = 0);

    public async Task<List<IntegrationEventMatch>> MatchAsync(
        Guid moduleId, Guid organizationId, IntegrationTrigger trigger, CancellationToken ct = default)
    {

        var scope = await dbContext.Modules
            .Where(m => m.Id == moduleId && m.OrganizationId == organizationId)
            .Select(m => new { m.NamespaceId, m.Namespace.StackId })
            .FirstOrDefaultAsync(ct);
        if (scope is null) return [];

        var module = await dbContext.Modules.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == moduleId && m.OrganizationId == organizationId, ct);

        var namespaceIds = module is { IgnoreInheritedIntegrationEvents: true }
            ? new List<Guid> { scope.NamespaceId }
            : await InheritedNamespaces.ForNamespace(
                dbContext, scope.NamespaceId, organizationId, n => n.IgnoreInheritedIntegrationEvents, ct);

        var candidates = new List<IntegrationEventMatch>();

        candidates.AddRange(await dbContext.OrganizationIntegrationEvents
            .Where(x => x.OrganizationId == organizationId && !x.IsDisabled && x.Trigger == trigger)
            .Select(x => new IntegrationEventMatch(x.Id, IntegrationEventScope.Organization, x.IntegrationId, x.Trigger, x.Template, x.Filter)).ToListAsync(ct));
        candidates.AddRange(await dbContext.StackIntegrationEvents
            .Where(x => x.OrganizationId == organizationId && x.StackId == scope.StackId && !x.IsDisabled && x.Trigger == trigger)
            .Select(x => new IntegrationEventMatch(x.Id, IntegrationEventScope.Stack, x.IntegrationId, x.Trigger, x.Template, x.Filter)).ToListAsync(ct));
        // The distance is the Namespace's position in the chain. Read the id and index it after the
        // query: a list lookup inside a projection is not translatable.
        var namespaceEvents = await dbContext.NamespaceIntegrationEvents
            .Where(x => x.OrganizationId == organizationId && namespaceIds.Contains(x.NamespaceId) && !x.IsDisabled && x.Trigger == trigger)
            .Select(x => new { x.Id, x.NamespaceId, x.IntegrationId, x.Trigger, x.Template, x.Filter })
            .ToListAsync(ct);

        candidates.AddRange(namespaceEvents.Select(x => new IntegrationEventMatch(
            x.Id, IntegrationEventScope.Namespace, x.IntegrationId, x.Trigger, x.Template, x.Filter,
            namespaceIds.IndexOf(x.NamespaceId))));
        candidates.AddRange(await dbContext.ModuleIntegrationEvents
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

        // Scope is declared least-to-most specific, so the highest value is the most specific
        // event. Within a scope, the nearest Namespace that declared one wins.
        return matched
            .GroupBy(m => m.IntegrationId)
            .Select(g => g.OrderByDescending(m => m.Scope).ThenBy(m => m.Distance).First())
            .ToList();
    }
}
