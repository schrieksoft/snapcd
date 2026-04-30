using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

namespace SnapCd.Server.Core.Database.ClassMaps.RoleAssignments.Org;

public class ServicePrincipalStackRoleAssignmentClassMap : IEntityTypeConfiguration<ServicePrincipalStackRoleAssignment>
{
    public void Configure(EntityTypeBuilder<ServicePrincipalStackRoleAssignment> entity)
    {
        // ServicePrincipal navigation property
        entity
            .HasOne(x => x.ServicePrincipal)
            .WithMany(x => x.ServicePrincipalStackRoleAssignments)
            .HasForeignKey("ServicePrincipalId", "OrganizationId")
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one role per service principal per stack
        entity
            .HasIndex(x => new { x.ServicePrincipalId, x.StackId, x.OrganizationId, x.RoleName })
            .IsUnique();

        // Index on ServicePrincipalId for lookups
        entity
            .HasIndex(x => x.ServicePrincipalId);

        // Enum conversion
        entity
            .Property(x => x.RoleName)
            .HasConversion<string>();
    }
}