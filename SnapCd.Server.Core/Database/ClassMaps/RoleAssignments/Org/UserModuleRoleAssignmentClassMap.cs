using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

namespace SnapCd.Server.Core.Database.ClassMaps.RoleAssignments.Org;

public class UserModuleRoleAssignmentClassMap : IEntityTypeConfiguration<UserModuleRoleAssignment>
{
    public void Configure(EntityTypeBuilder<UserModuleRoleAssignment> entity)
    {
        // OrganizationUser navigation property
        entity
            .HasOne(x => x.OrganizationUser)
            .WithMany(x => x.UserModuleRoleAssignments)
            .HasForeignKey("UserId", "OrganizationId")
            .HasPrincipalKey(x => new { x.UserId, x.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one role per user per module
        entity
            .HasIndex(x => new { x.UserId, x.ModuleId, x.OrganizationId, x.RoleName })
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