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

public class ModuleTerraformInlinePolicyClassMap : IEntityTypeConfiguration<ModuleTerraformInlinePolicy>
{
    public void Configure(EntityTypeBuilder<ModuleTerraformInlinePolicy> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        entity
            .HasIndex(p => new { p.ModuleId, p.Name })
            .IsUnique();

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.ModuleTerraformInlinePolicies)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(x => x.Module)
            .WithMany(x => x.TerraformInlinePolicies)
            .HasForeignKey(x => new { x.ModuleId, x.OrganizationId })
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
