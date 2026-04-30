using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class SourceRefresherPreselectionClassMap : IEntityTypeConfiguration<SourceRefresherPreselection>
{
    public void Configure(EntityTypeBuilder<SourceRefresherPreselection> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        entity
            .HasOne(e => e.Runner)
            .WithMany(x => x.SourceRefresherPreselections)
            .HasForeignKey(e => new { e.RunnerId, e.OrganizationId })
            .HasPrincipalKey(rp => new { rp.Id, rp.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.SourceRefresherPreselections)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasIndex(p => new { p.SourceUrl, p.OrganizationId })
            .IsUnique();
    }
}