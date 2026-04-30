using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class ServicePrincipalClassMap : IEntityTypeConfiguration<ServicePrincipal>
{
    public void Configure(EntityTypeBuilder<ServicePrincipal> entity)
    {
        // Primary key on Id only (for OpenIddict compatibility)
        entity.HasKey(e => e.Id);

        // Alternate key for domain entities to reference
        entity.HasAlternateKey(e => new { e.Id, e.OrganizationId });

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.ServicePrincipals)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint to ensure ClientId is unique within an organization
        entity
            .HasIndex(p => new { p.ClientId, p.OrganizationId })
            .IsUnique();

        // Index on OrganizationId for query performance
        entity
            .HasIndex(e => e.OrganizationId);
    }
}