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

public class StateStoreRoleAssignmentClassMap : IEntityTypeConfiguration<StateStoreRoleAssignment>
{
    public void Configure(EntityTypeBuilder<StateStoreRoleAssignment> entity)
    {
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.HasIndex(e => e.Id).IsUnique();

        entity.Property(e => e.PrincipalDiscriminator)
            .HasConversion<string>()
            .HasMaxLength(32);

        entity.HasDiscriminator(e => e.PrincipalDiscriminator)
            .HasValue<StateStoreRoleAssignment>(RoleAssignmentPrincipalDiscriminator.Base)
            .HasValue<UserStateStoreRoleAssignment>(RoleAssignmentPrincipalDiscriminator.User)
            .HasValue<ServicePrincipalStateStoreRoleAssignment>(RoleAssignmentPrincipalDiscriminator.ServicePrincipal)
            .HasValue<GroupStateStoreRoleAssignment>(RoleAssignmentPrincipalDiscriminator.Group);

        entity
            .Property(x => x.PrincipalId)
            .HasComputedColumnSql(
                "CASE " +
                "WHEN [PrincipalDiscriminator] = 'User' THEN [UserId] " +
                "WHEN [PrincipalDiscriminator] = 'ServicePrincipal' THEN [ServicePrincipalId] " +
                "WHEN [PrincipalDiscriminator] = 'Group' THEN [GroupId] " +
                "END",
                true);

        entity
            .HasIndex(e => e.PrincipalId);

        entity
            .HasIndex(e => new { e.StateStoreId, e.OrganizationId, e.PrincipalId, e.RoleName })
            .HasDatabaseName("IX_StateStoreRoleAssign_UserSP_StoreFirst")
            .HasFilter("[PrincipalDiscriminator] IN ('User', 'ServicePrincipal')");

        entity
            .HasIndex(e => new { e.PrincipalId, e.StateStoreId, e.OrganizationId, e.RoleName })
            .HasDatabaseName("IX_StateStoreRoleAssign_Group_PrincipalFirst")
            .HasFilter("[PrincipalDiscriminator] = 'Group'");

        entity
            .HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(e => e.StateStore)
            .WithMany(x => x.StateStoreRoleAssignments)
            .HasForeignKey("StateStoreId", "OrganizationId")
            .HasPrincipalKey(s => new { s.Id, s.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
