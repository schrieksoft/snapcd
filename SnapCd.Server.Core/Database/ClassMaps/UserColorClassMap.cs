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

public class UserColorClassMap : IEntityTypeConfiguration<UserColor>
{
    public void Configure(EntityTypeBuilder<UserColor> entity)
    {
        entity.ToTable("UserColors");

        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Hot path: list a user's colours within an organization (the dependency graph
        // fetches the whole set once and resolves against it in memory)
        entity.HasIndex(e => new { e.UserId, e.OrganizationId });

        // One colour per (user, target) — this is what makes assignment an upsert
        entity
            .HasIndex(e => new { e.UserId, e.OrganizationId, e.TargetType, e.TargetId })
            .IsUnique();

        entity
            .Property(e => e.TargetType)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Hex colour, e.g. "#E85D1A"
        entity
            .Property(e => e.Color)
            .IsRequired()
            .HasMaxLength(9);

        entity
            .HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
