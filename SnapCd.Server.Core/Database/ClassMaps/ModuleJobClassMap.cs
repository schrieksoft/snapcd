using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class ModuleJobClassMap : IEntityTypeConfiguration<ModuleJob>
{
    public ModuleJobClassMap()
    {
    }

    public void Configure(EntityTypeBuilder<ModuleJob> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.ModuleJobs)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasIndex(p => new { p.ModuleId, p.TimestampStart, p.OrganizationId })
            .IsUnique();

        entity
            .HasIndex(m => m.ModuleId);

        entity
            .HasIndex(m => new { m.ModuleId, m.TimestampEnd, m.OrganizationId });

        // Ensure only one ModuleJob per Module can be IsCurrent
        entity
            .HasIndex(m => new { m.ModuleId, m.IsCurrent, m.OrganizationId })
            .IsUnique()
            .HasFilter("[IsCurrent] = 1");

        entity
            .Property(d => d.Status)
            .HasConversion<string>();

        entity
            .HasOne(e => e.Module)
            .WithMany(u => u.ModuleJobs)
            .HasForeignKey(e => new { e.ModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .Property(d => d.FailedOnServerSideStep)
            .HasConversion<string>();

        entity
            .Property(d => d.ActualStateHeadline)
            .HasConversion<string>();
    }
}