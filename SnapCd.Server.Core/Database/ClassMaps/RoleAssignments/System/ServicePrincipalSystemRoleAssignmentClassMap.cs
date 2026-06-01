// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.System;

namespace SnapCd.Server.Core.Database.ClassMaps.RoleAssignments.System;

public class ServicePrincipalSystemRoleAssignmentClassMap : IEntityTypeConfiguration<ServicePrincipalSystemRoleAssignment>
{
    public void Configure(EntityTypeBuilder<ServicePrincipalSystemRoleAssignment> entity)
    {
        // Primary key
        entity.HasKey(x => x.Id);

        // Unique index on Id
        entity.HasIndex(x => x.Id).IsUnique();


        // ServicePrincipal navigation property (references primary key Id)
        entity
            .HasOne(x => x.ServicePrincipal)
            .WithMany(x => x.ServicePrincipalSystemRoleAssignments)
            .HasForeignKey(x => x.ServicePrincipalId)
            .OnDelete(DeleteBehavior.Cascade);


        // Unique constraint: one role per service principal per system
        entity
            .HasIndex(x => new { x.ServicePrincipalId, x.RoleName })
            .IsUnique();

        // Index on ServicePrincipalId for lookups
        entity
            .HasIndex(x => x.ServicePrincipalId);

        // Enum conversion
        entity
            .Property(x => x.RoleName)
            .HasConversion<string>();

        // Computed column for PrincipalId
        entity
            .Property(x => x.PrincipalId)
            .HasComputedColumnSql("[ServicePrincipalId]", false);
    }
}