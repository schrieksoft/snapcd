using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class DependsOnModuleClassMap : IEntityTypeConfiguration<DependsOnModule>
{
    public void Configure(EntityTypeBuilder<DependsOnModule> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.DependsOnModules)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasIndex(p => new { p.ModuleId, p.DependsOnModuleId, p.OrganizationId })
            .IsUnique();

        entity
            .HasOne(d => d.Module)
            .WithMany(x => x.DependsOnModules)
            .HasForeignKey(d => new { d.ModuleId, d.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne(d => d.DependsOnModuleNavigation)
            .WithMany(x => x.DependentModules)
            .HasForeignKey(d => new { d.DependsOnModuleId, d.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}