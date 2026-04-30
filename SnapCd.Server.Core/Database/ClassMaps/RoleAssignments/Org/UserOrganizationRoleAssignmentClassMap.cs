using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

namespace SnapCd.Server.Core.Database.ClassMaps.RoleAssignments.Org;

public class UserOrganizationRoleAssignmentClassMap : IEntityTypeConfiguration<UserOrganizationRoleAssignment>
{
    public void Configure(EntityTypeBuilder<UserOrganizationRoleAssignment> entity)
    {
        // OrganizationUser navigation property
        entity
            .HasOne(x => x.OrganizationUser)
            .WithMany(x => x.UserOrganizationRoleAssignments)
            .HasForeignKey("UserId", "OrganizationId")
            .HasPrincipalKey(x => new { x.UserId, x.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one role per user per organization
        entity
            .HasIndex(x => new { x.UserId, x.OrganizationId, x.RoleName })
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