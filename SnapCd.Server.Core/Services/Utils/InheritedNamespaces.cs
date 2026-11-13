// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;

namespace SnapCd.Server.Core.Services.Utils;

/// <summary>
/// The Namespaces a Module or Namespace inherits a given collection from: itself and each one
/// above it, stopping at the first that refuses what is above.
/// </summary>
public static class InheritedNamespaces
{
    /// <summary>
    /// The inheritance chain of every Namespace named, built in one pass. For a batch of Modules,
    /// where asking per Module would be a query each.
    /// </summary>
    public static async Task<Dictionary<Guid, List<Entities.Definition.Namespace>>> ForNamespaces(
        SnapCdDbContext dbContext,
        IReadOnlyCollection<Guid> namespaceIds,
        Guid organizationId,
        Func<IQueryable<Entities.Definition.Namespace>, IQueryable<Entities.Definition.Namespace>> include,
        CancellationToken ct = default)
    {
        if (namespaceIds.Count == 0) return [];

        var edges = await dbContext.NamespaceAncestors
            .AsNoTracking()
            .Where(a => namespaceIds.Contains(a.NamespaceId) && a.OrganizationId == organizationId)
            .Select(a => new { a.NamespaceId, a.AncestorNamespaceId, a.Depth })
            .ToListAsync(ct);

        var ancestorIds = edges.Select(e => e.AncestorNamespaceId).Distinct().ToList();

        var namespaces = await include(dbContext.Namespaces.AsNoTracking().AsSplitQuery())
            .Where(n => ancestorIds.Contains(n.Id) && n.OrganizationId == organizationId)
            .ToListAsync(ct);

        var byId = namespaces.ToDictionary(n => n.Id);

        return edges
            .GroupBy(e => e.NamespaceId)
            .ToDictionary(
                g => g.Key,
                g => g.OrderBy(e => e.Depth)
                    .Where(e => byId.ContainsKey(e.AncestorNamespaceId))
                    .Select(e => byId[e.AncestorNamespaceId])
                    .ToList());
    }

    /// <summary>
    /// Ancestor ids nearest first, cut off after the first Namespace whose <paramref name="ignores"/>
    /// is set. The Namespace itself is always included.
    /// </summary>
    public static async Task<List<Guid>> ForNamespace(
        SnapCdDbContext dbContext,
        Guid namespaceId,
        Guid organizationId,
        Func<Entities.Definition.Namespace, bool> ignores,
        CancellationToken ct = default)
    {
        var ordered = await dbContext.NamespaceAncestors
            .AsNoTracking()
            .Where(a => a.NamespaceId == namespaceId && a.OrganizationId == organizationId)
            .OrderBy(a => a.Depth)
            .Join(dbContext.Namespaces, a => a.AncestorNamespaceId, n => n.Id, (a, n) => new { a.Depth, Namespace = n })
            .ToListAsync(ct);

        var kept = new List<Guid>();

        foreach (var row in ordered.OrderBy(r => r.Depth))
        {
            kept.Add(row.Namespace.Id);
            if (ignores(row.Namespace)) break;
        }

        return kept;
    }
}
