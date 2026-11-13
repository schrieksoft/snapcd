// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Views;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class NamespaceResolvedDefaultClassMap : IEntityTypeConfiguration<NamespaceResolvedDefault>
{
    public void Configure(EntityTypeBuilder<NamespaceResolvedDefault> entity)
    {
        // One row per Namespace, so it has a key even though it is a view. A keyless type cannot be
        // the target of a navigation, and the approval queries reach it through one.
        entity.HasKey(e => new { e.NamespaceId, e.OrganizationId });
        entity.ToView("NamespaceResolvedDefaults");

        entity
            .HasOne<Entities.Definition.Namespace>()
            .WithMany(n => n.ResolvedDefaults)
            .HasForeignKey(d => new { d.NamespaceId, d.OrganizationId })
            .HasPrincipalKey(n => new { n.Id, n.OrganizationId })
            .OnDelete(DeleteBehavior.NoAction);

        entity.Property(e => e.Engine).HasConversion<string>();
        entity.Property(e => e.OutputSetDownstreamRestriction).HasConversion<string>();
    }
}
