using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.System;

namespace SnapCd.Server.Core.Database.ClassMaps.RoleAssignments.System;

public class ServicePrincipalSystemRoleAssignmentClassMap : IEntityTypeConfiguration<ServicePrincipalSystemRoleAssignment>
{
    public void Configure(EntityTypeBuilder<ServicePrincipalSystemRoleAssignment> entity)
    {
        // Primary key
        entity.HasKey(x => x.Id);

        // Unique index on Id
        entity.HasIndex(x => x.Id).IsUnique();


        // ServicePrincipal navigation property (references primary key Id)
        entity
            .HasOne(x => x.ServicePrincipal)
            .WithMany(x => x.ServicePrincipalSystemRoleAssignments)
            .HasForeignKey(x => x.ServicePrincipalId)
            .OnDelete(DeleteBehavior.Cascade);


        // Unique constraint: one role per service principal per system
        entity
            .HasIndex(x => new { x.ServicePrincipalId, x.RoleName })
            .IsUnique();

        // Index on ServicePrincipalId for lookups
        entity
            .HasIndex(x => x.ServicePrincipalId);

        // Enum conversion
        entity
            .Property(x => x.RoleName)
            .HasConversion<string>();

        // Computed column for PrincipalId
        entity
            .Property(x => x.PrincipalId)
            .HasComputedColumnSql("[ServicePrincipalId]", false);
    }
}