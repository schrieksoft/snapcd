using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class RunnerClassMap : IEntityTypeConfiguration<Runner>
{
    public void Configure(EntityTypeBuilder<Runner> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        entity
            .HasIndex(p => new { p.Name, p.OrganizationId })
            .IsUnique();

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.Runners)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // ServicePrincipal navigation property with composite FK
        entity
            .HasOne(e => e.ServicePrincipal)
            .WithMany(x => x.Runners)
            .HasForeignKey(e => new { e.ServicePrincipalId, e.OrganizationId })
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        // Index on ServicePrincipalId for lookups
        entity.HasIndex(e => e.ServicePrincipalId);
    }
}