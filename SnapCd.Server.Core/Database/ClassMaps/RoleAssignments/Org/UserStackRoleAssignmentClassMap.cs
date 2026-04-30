using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

namespace SnapCd.Server.Core.Database.ClassMaps.RoleAssignments.Org;

public class UserStackRoleAssignmentClassMap : IEntityTypeConfiguration<UserStackRoleAssignment>
{
    public void Configure(EntityTypeBuilder<UserStackRoleAssignment> entity)
    {
        // OrganizationUser navigation property
        entity
            .HasOne(x => x.OrganizationUser)
            .WithMany(x => x.UserStackRoleAssignments)
            .HasForeignKey("UserId", "OrganizationId")
            .HasPrincipalKey(x => new { x.UserId, x.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one role per user per stack
        entity
            .HasIndex(x => new { x.UserId, x.StackId, x.OrganizationId, x.RoleName })
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