using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

namespace SnapCd.Server.Core.Database.ClassMaps.RoleAssignments.Org;

public class ServicePrincipalOrganizationRoleAssignmentClassMap : IEntityTypeConfiguration<ServicePrincipalOrganizationRoleAssignment>
{
    public void Configure(EntityTypeBuilder<ServicePrincipalOrganizationRoleAssignment> entity)
    {
        // ServicePrincipal navigation property
        entity
            .HasOne(x => x.ServicePrincipal)
            .WithMany(x => x.ServicePrincipalOrganizationRoleAssignments)
            .HasForeignKey("ServicePrincipalId", "OrganizationId")
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one role per service principal per organization
        entity
            .HasIndex(x => new { x.ServicePrincipalId, x.OrganizationId, x.RoleName })
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