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

public class NamespaceClassMap : IEntityTypeConfiguration<Namespace>
{
    public void Configure(EntityTypeBuilder<Namespace> entity)
    {
        // Namespaces has a trigger (trg_Namespaces_ClosureNames), so EF must not use the OUTPUT clause
        entity.ToTable("Namespaces", t => t.UseSqlOutputClause(false));

        // Composite Primary Key
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity
            .HasIndex(p => new { p.CreatedDateTime });

        entity
            .Property(d => d.DefaultEngine)
            .HasConversion<string>()
            .HasMaxLength(50);

        entity
            .Property(d => d.TriggerBehaviourOnModified)
            .HasConversion<string>();

        entity
            .Property(d => d.DefaultOutputSetDownstreamRestriction)
            .HasConversion<string>()
            .HasMaxLength(50);

        entity
            .Property(p => p.CreatedDateTime)
            .ValueGeneratedOnAdd();

        entity
            .Property(p => p.Number)
            .UseIdentityColumn();

        entity
            .HasIndex(p => p.Number)
            .IsUnique();

        // Foreign Key to Organization
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.Namespaces)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign Key to Stack (composite)
        entity
            .HasOne(e => e.Stack)
            .WithMany(e => e.Namespaces)
            .HasForeignKey(e => new { e.StackId, e.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // Updated unique index to include OrganizationId
        entity
            .HasIndex(i => new { i.OrganizationId, i.StackId, i.Name })
            .IsUnique();

        entity
            .HasIndex(n => n.StackId);

        // Navigation optimization index for reverse inherited permissions
        // Critical for Namespace -> Stack traversal performance
        entity
            .HasIndex(n => new { n.StackId, n.Id });

        // Restrict, not Cascade: Stack already cascades into Namespaces and SQL Server allows one
        // cascade path into a table. NamespaceRepository clears the children itself.
        entity
            .HasOne(e => e.ParentNamespace)
            .WithMany(e => e.ChildNamespaces)
            .HasForeignKey(e => new { e.ParentNamespaceId, e.OrganizationId })
            .HasPrincipalKey(e => new { e.Id, e.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasIndex(n => new { n.ParentNamespaceId, n.OrganizationId });

        // Unique index on Id field
        entity
            .HasIndex(e => e.Id)
            .IsUnique();

        // Updated relationship to Modules with composite FK
        entity
            .HasMany(e => e.Modules)
            .WithOne(e => e.Namespace)
            .HasForeignKey(e => new { e.NamespaceId, e.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity.Ignore(e => e.GroupNamespaceRoleAssignments);
        entity.Ignore(e => e.UserNamespaceRoleAssignments);
        entity.Ignore(e => e.ServicePrincipalNamespaceRoleAssignments);
    }
}