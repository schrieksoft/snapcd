// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.Base;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class NamespaceInputClassMap : IEntityTypeConfiguration<NamespaceInput>
{
    public void Configure(EntityTypeBuilder<NamespaceInput> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .Property(d => d.UsageMode)
            .HasConversion<string>();

        // Configure InputKind to store as string
        entity
            .Property(e => e.InputKind)
            .HasConversion<string>();

        // Unique index on NamespaceId, InputKind, and Name combination
        entity
            .HasIndex(p => new { p.NamespaceId, p.InputKind, p.Name, p.OrganizationId })
            .IsUnique();

        // Foreign key index
        entity
            .HasIndex(p => p.NamespaceId);
    }
}