using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.System;

namespace SnapCd.Server.Core.Database.ClassMaps.RoleAssignments.System;

public class UserSystemRoleAssignmentClassMap : IEntityTypeConfiguration<UserSystemRoleAssignment>
{
    public void Configure(EntityTypeBuilder<UserSystemRoleAssignment> entity)
    {
        // Primary key
        entity.HasKey(x => x.Id);

        // Unique index on Id
        entity.HasIndex(x => x.Id).IsUnique();

        // User navigation property
        entity
            .HasOne(x => x.User)
            .WithMany(x => x.UserSystemRoleAssignments)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one role per user per system
        entity
            .HasIndex(x => new { x.UserId, x.RoleName })
            .IsUnique();

        // Index on UserId for lookups
        entity
            .HasIndex(x => x.UserId);

        // Enum conversion
        entity
            .Property(x => x.RoleName)
            .HasConversion<string>();

        // Computed column for PrincipalId
        entity
            .Property(x => x.PrincipalId)
            .HasComputedColumnSql("[UserId]", false);
    }
}