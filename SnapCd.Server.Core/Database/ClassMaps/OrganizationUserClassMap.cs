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

public class OrganizationUserClassMap : IEntityTypeConfiguration<OrganizationUser>
{
    public void Configure(EntityTypeBuilder<OrganizationUser> entity)
    {
        entity.HasKey(e => new { e.UserId, e.OrganizationId });

        entity.HasAlternateKey(e => e.Id);

        entity
            .Property(e => e.OrganizationId)
            .IsRequired();

        entity
            .Property(e => e.UserId);


        entity
            .Property(e => e.JoinedAt)
            .ValueGeneratedOnAdd()
            .IsRequired();

        entity
            .Property(e => e.IsDeactivated)
            .IsRequired()
            .HasDefaultValue(false);

        entity
            .Property(e => e.InvitationToken)
            .HasMaxLength(255);

        entity
            .Property(e => e.InvitationCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes
        entity
            .HasIndex(e => new { e.OrganizationId, e.UserId })
            .IsUnique()
            .HasFilter("UserId IS NOT NULL");

        entity
            .HasIndex(e => e.InvitationToken)
            .IsUnique()
            .HasFilter("InvitationToken IS NOT NULL");

        entity
            .HasIndex(e => e.OrganizationId);

        entity
            .HasIndex(e => e.UserId);

        entity
            .HasIndex(e => e.JoinedAt);

        // Foreign keys
        entity
            .HasOne(e => e.Organization)
            .WithMany(e => e.OrganizationUsers)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(e => e.User)
            .WithMany(e => e.OrganizationUsers)
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}