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

public class UserRunnerRoleAssignmentClassMap : IEntityTypeConfiguration<UserRunnerRoleAssignment>
{
    public void Configure(EntityTypeBuilder<UserRunnerRoleAssignment> entity)
    {
        // OrganizationUser navigation property
        entity
            .HasOne(x => x.OrganizationUser)
            .WithMany(x => x.UserRunnerRoleAssignments)
            .HasForeignKey("UserId", "OrganizationId")
            .HasPrincipalKey(x => new { x.UserId, x.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one role per user per Runner
        entity
            .HasIndex(x => new { x.UserId, x.RunnerId, x.OrganizationId, x.RoleName })
            .IsUnique();

        // Index on UserId for lookups
        entity
            .HasIndex(x => x.UserId);

        // Enum conversion
        entity
            .Property(x => x.RoleName)
            .HasConversion<string>();
    }
}