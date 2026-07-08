// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class StateFileClassMap : IEntityTypeConfiguration<StateFile>
{
    public void Configure(EntityTypeBuilder<StateFile> entity)
    {
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity
            .HasIndex(e => e.Id)
            .IsUnique();

        entity
            .HasIndex(e => new { e.StateStoreId, e.Name })
            .IsUnique();

        entity
            .HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(e => e.StateStore)
            .WithMany(e => e.StateFiles)
            .HasForeignKey(e => new { e.StateStoreId, e.OrganizationId })
            .HasPrincipalKey(s => new { s.Id, s.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .Property(e => e.LockedByPrincipalDiscriminator)
            .HasConversion<string>();

        entity
            .Property(e => e.RowVersion)
            .IsRowVersion();
    }
}
