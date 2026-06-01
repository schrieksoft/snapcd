// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

namespace SnapCd.Server.Core.Database.ClassMaps.RoleAssignments.Org;

public class GroupOrganizationRoleAssignmentClassMap : IEntityTypeConfiguration<GroupOrganizationRoleAssignment>
{
    public void Configure(EntityTypeBuilder<GroupOrganizationRoleAssignment> entity)
    {
        // Group navigation property
        entity
            .HasOne(x => x.Group)
            .WithMany(x => x.GroupOrganizationRoleAssignments)
            .HasForeignKey("GroupId", "OrganizationId")
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one role per group per organization
        entity
            .HasIndex(x => new { x.GroupId, x.OrganizationId, x.RoleName })
            .IsUnique();

        // Index on GroupId for lookups
        entity
            .HasIndex(x => x.GroupId);

        // Enum conversion
        entity
            .Property(x => x.RoleName)
            .HasConversion<string>();
    }
}