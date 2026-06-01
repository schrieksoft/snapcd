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

public class StackClassMap : IEntityTypeConfiguration<Stack>
{
    public void Configure(EntityTypeBuilder<Stack> entity)
    {
        // Composite Primary Key
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity
            .Property(o => o.CreatedDateTime)
            .ValueGeneratedOnAdd();

        entity
            .Property(d => d.TriggerBehaviourOnModified)
            .HasConversion<string>();

        // Foreign Key to Organization
        entity
            .HasOne(e => e.Organization)
            .WithMany(e => e.Stacks)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Updated unique index to include OrganizationId
        entity
            .HasIndex(p => new { p.OrganizationId, p.Name })
            .IsUnique();

        entity
            .HasIndex(p => new { p.CreatedDateTime });

        // Unique index on Id field
        entity
            .HasIndex(e => e.Id)
            .IsUnique();

        // Updated relationship to Namespaces with composite FK
        entity
            .HasMany(e => e.Namespaces)
            .WithOne(e => e.Stack)
            .HasForeignKey(e => new { e.StackId, e.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasMany(store => store.SecretsScopedToStack)
            .WithOne(secret => secret.Stack)
            .HasForeignKey(secret => new { secret.StackId, secret.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity.Ignore(e => e.GroupStackRoleAssignments);
        entity.Ignore(e => e.UserStackRoleAssignments);
        entity.Ignore(e => e.ServicePrincipalStackRoleAssignments);
    }
}