// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.Signals;

namespace SnapCd.Server.Core.Database.ClassMaps.Signals;

public class ResourceChangeClassMap : IEntityTypeConfiguration<ResourceChange>
{
    public void Configure(EntityTypeBuilder<ResourceChange> entity)
    {
        entity.ToTable("ResourceChanges", t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.HasIndex(e => e.Id).IsUnique();

        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.ResourceChanges)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(e => e.ResourceChangeSet)
            .WithMany(x => x.ResourceChanges)
            .HasForeignKey(e => new { e.ResourceChangeSetId, e.OrganizationId })
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasIndex(p => new { p.ResourceChangeSetId, p.Address, p.OrganizationId })
            .IsUnique();
    }
}
