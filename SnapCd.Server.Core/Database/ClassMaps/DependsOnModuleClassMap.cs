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

public class DependsOnModuleClassMap : IEntityTypeConfiguration<DependsOnModule>
{
    public void Configure(EntityTypeBuilder<DependsOnModule> entity)
    {
        entity.ToTable("DependsOnModules", t => t.UseSqlOutputClause(false));

        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.DependsOnModules)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasIndex(p => new { p.ModuleId, p.DependsOnModuleId, p.OrganizationId })
            .IsUnique();

        entity
            .HasOne(d => d.Module)
            .WithMany(x => x.DependsOnModules)
            .HasForeignKey(d => new { d.ModuleId, d.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne(d => d.DependsOnModuleNavigation)
            .WithMany(x => x.DependentModules)
            .HasForeignKey(d => new { d.DependsOnModuleId, d.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}