using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

namespace SnapCd.Server.Core.Database.ClassMaps.RoleAssignments.Org;

public class GroupStackRoleAssignmentClassMap : IEntityTypeConfiguration<GroupStackRoleAssignment>
{
    public void Configure(EntityTypeBuilder<GroupStackRoleAssignment> entity)
    {
        // Group navigation property
        entity
            .HasOne(x => x.Group)
            .WithMany(x => x.GroupStackRoleAssignments)
            .HasForeignKey("GroupId", "OrganizationId")
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one role per group per stack
        entity
            .HasIndex(x => new { x.GroupId, x.StackId, x.OrganizationId, x.RoleName })
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