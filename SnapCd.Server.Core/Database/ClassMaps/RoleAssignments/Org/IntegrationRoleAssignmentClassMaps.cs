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
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Integration.Base;

namespace SnapCd.Server.Core.Database.ClassMaps.RoleAssignments.Org;

public class IntegrationRoleAssignmentClassMap : IEntityTypeConfiguration<IntegrationRoleAssignment>
{
    public void Configure(EntityTypeBuilder<IntegrationRoleAssignment> entity)
    {
        entity.HasKey(e => new { e.Id, e.OrganizationId });
        entity.HasIndex(e => e.Id).IsUnique();

        entity.Property(e => e.PrincipalDiscriminator).HasConversion<string>().HasMaxLength(32);
        entity.Property(e => e.RoleName).HasConversion<string>().HasMaxLength(64);

        entity.HasDiscriminator(e => e.PrincipalDiscriminator)
            .HasValue<IntegrationRoleAssignment>(RoleAssignmentPrincipalDiscriminator.Base)
            .HasValue<UserIntegrationRoleAssignment>(RoleAssignmentPrincipalDiscriminator.User)
            .HasValue<ServicePrincipalIntegrationRoleAssignment>(RoleAssignmentPrincipalDiscriminator.ServicePrincipal)
            .HasValue<GroupIntegrationRoleAssignment>(RoleAssignmentPrincipalDiscriminator.Group);

        entity.Property(x => x.PrincipalId)
            .HasComputedColumnSql(
                "CASE " +
                "WHEN [PrincipalDiscriminator] = 'User' THEN [UserId] " +
                "WHEN [PrincipalDiscriminator] = 'ServicePrincipal' THEN [ServicePrincipalId] " +
                "WHEN [PrincipalDiscriminator] = 'Group' THEN [GroupId] " +
                "END",
                true);

        entity.HasIndex(e => e.PrincipalId);
        entity.HasIndex(e => e.IntegrationId);

        // Composite index for permission query optimization
        entity
            .HasIndex(e => new { e.IntegrationId, e.OrganizationId, e.PrincipalId, e.RoleName })
            .HasDatabaseName("IX_IntegRoleAssign_Integ_Principal_Org_Role");

        entity.HasOne(e => e.Organization).WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Integration).WithMany(i => i.IntegrationRoleAssignments)
            .HasForeignKey("IntegrationId", "OrganizationId")
            .HasPrincipalKey(i => new { i.Id, i.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class UserIntegrationRoleAssignmentClassMap : IEntityTypeConfiguration<UserIntegrationRoleAssignment>
{
    public void Configure(EntityTypeBuilder<UserIntegrationRoleAssignment> entity)
    {
        entity.HasIndex(x => new { x.UserId, x.IntegrationId, x.OrganizationId, x.RoleName }).IsUnique();
        entity.HasIndex(x => x.UserId);
    }
}

public class ServicePrincipalIntegrationRoleAssignmentClassMap : IEntityTypeConfiguration<ServicePrincipalIntegrationRoleAssignment>
{
    public void Configure(EntityTypeBuilder<ServicePrincipalIntegrationRoleAssignment> entity)
    {
        entity.HasIndex(x => new { x.ServicePrincipalId, x.IntegrationId, x.OrganizationId, x.RoleName }).IsUnique();
        entity.HasIndex(x => x.ServicePrincipalId);
    }
}

public class GroupIntegrationRoleAssignmentClassMap : IEntityTypeConfiguration<GroupIntegrationRoleAssignment>
{
    public void Configure(EntityTypeBuilder<GroupIntegrationRoleAssignment> entity)
    {
        entity.HasIndex(x => new { x.GroupId, x.IntegrationId, x.OrganizationId, x.RoleName }).IsUnique();
        entity.HasIndex(x => x.GroupId);
    }
}
