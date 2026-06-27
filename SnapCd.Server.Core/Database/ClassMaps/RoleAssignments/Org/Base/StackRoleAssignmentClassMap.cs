// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Base;

namespace SnapCd.Server.Core.Database.ClassMaps.RoleAssignments.Org.Base;

public class StackRoleAssignmentClassMap : IEntityTypeConfiguration<StackRoleAssignment>
{
    public void Configure(EntityTypeBuilder<StackRoleAssignment> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Configure RoleAssignmentPrincipalDiscriminator to be stored as string with max length
        entity.Property(e => e.PrincipalDiscriminator)
            .HasConversion<string>()
            .HasMaxLength(32);

        // Configure TPH (Table Per Hierarchy) inheritance strategy with discriminator
        entity.HasDiscriminator(e => e.PrincipalDiscriminator)
            .HasValue<StackRoleAssignment>(RoleAssignmentPrincipalDiscriminator.Base) // This value won't be used, just for base
            .HasValue<UserStackRoleAssignment>(RoleAssignmentPrincipalDiscriminator.User)
            .HasValue<ServicePrincipalStackRoleAssignment>(RoleAssignmentPrincipalDiscriminator.ServicePrincipal)
            .HasValue<GroupStackRoleAssignment>(RoleAssignmentPrincipalDiscriminator.Group);

        // Computed column for PrincipalId based on discriminator (stored for indexing)
        entity
            .Property(x => x.PrincipalId)
            .HasComputedColumnSql(
                "CASE " +
                "WHEN [PrincipalDiscriminator] = 'User' THEN [UserId] " +
                "WHEN [PrincipalDiscriminator] = 'ServicePrincipal' THEN [ServicePrincipalId] " +
                "WHEN [PrincipalDiscriminator] = 'Group' THEN [GroupId] " +
                "END",
                true);

        // Index on PrincipalId for efficient lookups
        entity
            .HasIndex(e => e.PrincipalId);

        // Index on StackId for efficient lookups
        entity
            .HasIndex(e => e.StackId);

        // Filtered composite indexes for permission query optimization.
        // User/SP queries join on StackId first; Group queries join on PrincipalId first.
        // Two filtered indexes instead of one unfiltered index prevent the optimizer from
        // choosing a bad join path for the group branch.
        entity
            .HasIndex(e => new { e.StackId, e.OrganizationId, e.PrincipalId, e.RoleName })
            .HasDatabaseName("IX_StackRoleAssign_UserSP_StackFirst")
            .HasFilter("[PrincipalDiscriminator] IN ('User', 'ServicePrincipal')");

        entity
            .HasIndex(e => new { e.PrincipalId, e.StackId, e.OrganizationId, e.RoleName })
            .HasDatabaseName("IX_StackRoleAssign_Group_PrincipalFirst")
            .HasFilter("[PrincipalDiscriminator] = 'Group'");

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.StackRoleAssignments)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Stack navigation property
        entity
            .HasOne(e => e.Stack)
            .WithMany(x => x.StackRoleAssignments)
            .HasForeignKey("StackId", "OrganizationId")
            .HasPrincipalKey(s => new { s.Id, s.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}