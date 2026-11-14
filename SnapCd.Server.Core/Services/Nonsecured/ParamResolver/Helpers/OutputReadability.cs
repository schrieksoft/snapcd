// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.Outputs;

namespace SnapCd.Server.Core.Services.Nonsecured.ParamResolver.Helpers;

/// <summary>
/// Whether one Module may read another's Output, as a single query.
///
/// The rule in one place: a supply to the consuming Module or its Namespace allows an Output
/// outright; otherwise the producer's restriction withholds it, unless the producer's exemption
/// covers where the consumer sits. The producer's rule is its own where it sets one, and the
/// Namespace chain's where it does not.
///
/// Written as one expression so it reaches the database. Every caller gets the same rule, and the
/// ripple and trigger lookups filter in SQL rather than afterwards. It cannot be split into helper
/// methods: EF translates an expression tree, not a call into C#.
/// </summary>
public static class OutputReadability
{
    /// <summary>
    /// The Outputs <paramref name="consumingModuleId"/> may read. Intersect with this rather than
    /// filtering a materialised list.
    /// </summary>
    public static IQueryable<Output> Readable(
        SnapCdDbContext dbContext,
        Guid consumingModuleId,
        Guid organizationId)
    {
        var consumingNamespaceId = dbContext.Modules
            .Where(m => m.Id == consumingModuleId && m.OrganizationId == organizationId)
            .Select(m => m.NamespaceId);

        return dbContext.Set<Output>()
            .Where(o => o.OrganizationId == organizationId)
            .Where(o =>
                // Supplied outright: the whole Output Set, to the Module or to its Namespace.
                dbContext.OutputSetModuleSupplies.Any(s =>
                    s.OrganizationId == organizationId
                    && s.ConsumingModuleId == consumingModuleId
                    && s.ModuleId == o.OutputSet.ModuleId)
                || dbContext.OutputSetNamespaceSupplies.Any(s =>
                    s.OrganizationId == organizationId
                    && consumingNamespaceId.Contains(s.ConsumingNamespaceId)
                    && s.ModuleId == o.OutputSet.ModuleId)

                // Supplied outright: this one Output, to the Module or to its Namespace.
                || dbContext.OutputModuleSupplies.Any(s =>
                    s.OrganizationId == organizationId
                    && s.ConsumingModuleId == consumingModuleId
                    && s.ModuleId == o.OutputSet.ModuleId
                    && s.OutputName == o.Name)
                || dbContext.OutputNamespaceSupplies.Any(s =>
                    s.OrganizationId == organizationId
                    && consumingNamespaceId.Contains(s.ConsumingNamespaceId)
                    && s.ModuleId == o.OutputSet.ModuleId
                    && s.OutputName == o.Name)

                // Not withheld at all.
                || (!((o.OutputSet.Module.OutputSetDownstreamRestriction
                           ?? o.OutputSet.Module.Namespace.ResolvedDefaults
                               .Where(d => d.OrganizationId == organizationId)
                               .Select(d => d.OutputSetDownstreamRestriction)
                               .FirstOrDefault()) == OutputSetDownstreamRestriction.All)
                    && !((o.OutputSet.Module.OutputSetDownstreamRestriction
                              ?? o.OutputSet.Module.Namespace.ResolvedDefaults
                                  .Where(d => d.OrganizationId == organizationId)
                                  .Select(d => d.OutputSetDownstreamRestriction)
                                  .FirstOrDefault()) == OutputSetDownstreamRestriction.SensitiveOnly
                         && o is SecretOutput)
                    && !dbContext.ModuleOutputDownstreamRestrictions.Any(r =>
                        r.OrganizationId == organizationId
                        && r.ModuleId == o.OutputSet.ModuleId
                        && r.OutputName == o.Name))

                // Withheld, but the consumer sits where the exemption allows.
                || ((o.OutputSet.Module.OutputRestrictionExemption
                         ?? o.OutputSet.Module.Namespace.ResolvedDefaults
                             .Where(d => d.OrganizationId == organizationId)
                             .Select(d => d.OutputRestrictionExemption)
                             .FirstOrDefault()) == OutputRestrictionExemption.OwnNamespace
                    && consumingNamespaceId.Contains(o.OutputSet.Module.NamespaceId))
                || ((o.OutputSet.Module.OutputRestrictionExemption
                         ?? o.OutputSet.Module.Namespace.ResolvedDefaults
                             .Where(d => d.OrganizationId == organizationId)
                             .Select(d => d.OutputRestrictionExemption)
                             .FirstOrDefault()) == OutputRestrictionExemption.OwnNamespaceAndDescendants
                    && dbContext.NamespaceAncestors.Any(a =>
                        a.OrganizationId == organizationId
                        && consumingNamespaceId.Contains(a.NamespaceId)
                        && a.AncestorNamespaceId == o.OutputSet.Module.NamespaceId)));
    }

    /// <summary>
    /// The same rule asked of a row rather than of one consumer: may <paramref name="consumerId"/>
    /// read the Output called <paramref name="outputName"/> from <paramref name="producerId"/>?
    ///
    /// For queries whose consumer varies per row - which Modules a changed Output reaches - where
    /// the per-consumer overload cannot be used. Kept beside it so the two stay the same rule.
    /// </summary>
    public static IQueryable<Guid> ReadableConsumerIds(
        SnapCdDbContext dbContext,
        Guid producerId,
        Guid organizationId,
        IEnumerable<Guid>? candidateConsumerIds,
        string? outputName)
    {
        var candidates = candidateConsumerIds?.ToList();

        // Asked of the producing Module rather than of one Output row, because a caller here is
        // deciding who a change reaches and the Outputs may not be stored yet.
        return dbContext.Modules
            .Where(m => (candidates == null || candidates.Contains(m.Id)) && m.OrganizationId == organizationId)
            .Where(m => dbContext.Modules.Any(producer =>
                producer.Id == producerId
                && producer.OrganizationId == organizationId

                // Supplied outright, to the Module or to its Namespace.
                && (dbContext.OutputSetModuleSupplies.Any(s =>
                        s.OrganizationId == organizationId
                        && s.ConsumingModuleId == m.Id
                        && s.ModuleId == producerId)
                    || dbContext.OutputSetNamespaceSupplies.Any(s =>
                        s.OrganizationId == organizationId
                        && s.ConsumingNamespaceId == m.NamespaceId
                        && s.ModuleId == producerId)
                    || (outputName != null && dbContext.OutputModuleSupplies.Any(s =>
                        s.OrganizationId == organizationId
                        && s.ConsumingModuleId == m.Id
                        && s.ModuleId == producerId
                        && s.OutputName == outputName))
                    || (outputName != null && dbContext.OutputNamespaceSupplies.Any(s =>
                        s.OrganizationId == organizationId
                        && s.ConsumingNamespaceId == m.NamespaceId
                        && s.ModuleId == producerId
                        && s.OutputName == outputName))

                    // Nothing withheld: not the whole set, and not this Output by name. Sensitivity
                    // is not known without the Output row, so SensitiveOnly withholds nothing here
                    // and the resolver refuses the value when it is actually read.
                    || (!((producer.OutputSetDownstreamRestriction
                               ?? producer.Namespace.ResolvedDefaults
                                   .Where(d => d.OrganizationId == organizationId)
                                   .Select(d => d.OutputSetDownstreamRestriction)
                                   .FirstOrDefault()) == OutputSetDownstreamRestriction.All)
                        && !(outputName != null && dbContext.ModuleOutputDownstreamRestrictions.Any(r =>
                            r.OrganizationId == organizationId
                            && r.ModuleId == producerId
                            && r.OutputName == outputName)))

                    // Withheld, but the consumer sits where the exemption allows.
                    || ((producer.OutputRestrictionExemption
                             ?? producer.Namespace.ResolvedDefaults
                                 .Where(d => d.OrganizationId == organizationId)
                                 .Select(d => d.OutputRestrictionExemption)
                                 .FirstOrDefault()) == OutputRestrictionExemption.OwnNamespace
                        && m.NamespaceId == producer.NamespaceId)
                    || ((producer.OutputRestrictionExemption
                             ?? producer.Namespace.ResolvedDefaults
                                 .Where(d => d.OrganizationId == organizationId)
                                 .Select(d => d.OutputRestrictionExemption)
                                 .FirstOrDefault()) == OutputRestrictionExemption.OwnNamespaceAndDescendants
                        && dbContext.NamespaceAncestors.Any(a =>
                            a.OrganizationId == organizationId
                            && a.NamespaceId == m.NamespaceId
                            && a.AncestorNamespaceId == producer.NamespaceId)))))
            .Select(m => m.Id);
    }
}
