using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class OutputClassMap : IEntityTypeConfiguration<Output>
{
    public void Configure(EntityTypeBuilder<Output> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.Outputs)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(t => t.OutputSet)
            .WithMany(sp => sp.Outputs)
            .HasForeignKey(store => new { store.OutputSetId, store.OrganizationId })
            .HasPrincipalKey(sp => new { sp.Id, sp.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasIndex(p => new { p.OutputSetId, p.Name, p.OrganizationId })
            .IsUnique();

        entity
            .HasIndex(o => o.OutputSetId);
    }
}