using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Base;

namespace SnapCd.Server.Core.Database.ClassMaps.RoleAssignments.Org.Base;

public class OrganizationRoleAssignmentClassMap : IEntityTypeConfiguration<OrganizationRoleAssignment>
{
    public void Configure(EntityTypeBuilder<OrganizationRoleAssignment> entity)
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
            .HasValue<OrganizationRoleAssignment>(RoleAssignmentPrincipalDiscriminator.Base) // This value won't be used, just for base
            .HasValue<UserOrganizationRoleAssignment>(RoleAssignmentPrincipalDiscriminator.User)
            .HasValue<ServicePrincipalOrganizationRoleAssignment>(RoleAssignmentPrincipalDiscriminator.ServicePrincipal)
            .HasValue<GroupOrganizationRoleAssignment>(RoleAssignmentPrincipalDiscriminator.Group);

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

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.OrganizationRoleAssignments)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}