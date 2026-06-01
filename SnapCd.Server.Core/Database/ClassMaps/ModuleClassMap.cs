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
using SnapCd.Server.Core.Entities.Sagas;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class ModuleClassMap : IEntityTypeConfiguration<Module>
{
    public void Configure(EntityTypeBuilder<Module> entity)
    {
        // Composite Primary Key
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity
            .Property(p => p.CreatedDateTime)
            .ValueGeneratedOnAdd();

        // Foreign Key to Organization
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.Modules)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign Key to Namespace (composite)
        entity
            .HasOne(e => e.Namespace)
            .WithMany(e => e.Modules)
            .HasForeignKey(e => new { e.NamespaceId, e.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // Updated unique index to include OrganizationId
        entity
            .HasIndex(m => new { m.OrganizationId, m.NamespaceId, m.Name })
            .IsUnique();

        entity
            .HasIndex(p => new { p.CreatedDateTime });

        entity
            .HasIndex(m => m.NamespaceId);

        entity
            .HasIndex(m => m.RunnerId);

        // Index for trigger-based queries
        entity
            .HasIndex(m => m.TriggerOnUpstreamOutputChanged);

        // Navigation optimization index for reverse inherited permissions
        // Critical for Module -> Namespace -> Stack traversal performance
        entity
            .HasIndex(m => new { m.NamespaceId, m.Id });

        // Unique index on Id field
        entity
            .HasIndex(e => e.Id)
            .IsUnique();

        entity
            .Property(d => d.Engine)
            .HasConversion<string>()
            .HasMaxLength(50);

        entity
            .Property(d => d.SourceType)
            .HasConversion<string>();

        entity
            .Property(d => d.SourceRevisionType)
            .HasConversion<string>();

        entity
            .Property(d => d.WaitForApplyDependencies)
            .HasConversion<string>()
            .HasMaxLength(50);

        entity
            .Property(d => d.WaitForDestroyDependencies)
            .HasConversion<string>()
            .HasMaxLength(50);

        // Foreign Key to Runner (composite)
        entity
            .HasOne(x => x.Runner)
            .WithMany(x => x.Modules)
            .HasForeignKey(m => new { m.RunnerId, m.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign Key to ModuleModifiedSaga (optional, uses Module.Id as FK)
        entity
            .HasOne(m => m.ModuleModifiedSaga)
            .WithOne(s => s.Module)
            .HasForeignKey<Module>(m => new { ModuleModifiedSagaCorrelationId = m.Id, ModuleModifiedSagaOrganizationId = m.OrganizationId })
            .HasPrincipalKey<ModuleModifiedSaga>(s => new { s.CorrelationId, s.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        entity.Ignore(e => e.GroupModuleRoleAssignments);
        entity.Ignore(e => e.UserModuleRoleAssignments);
        entity.Ignore(e => e.ServicePrincipalModuleRoleAssignments);
    }
}