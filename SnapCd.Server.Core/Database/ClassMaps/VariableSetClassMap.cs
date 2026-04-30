using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class VariableSetClassMap : IEntityTypeConfiguration<VariableSet>
{
    public void Configure(EntityTypeBuilder<VariableSet> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.VariableSets)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasIndex(p => new { p.ModuleId, p.Timestamp, p.Checksum, p.OrganizationId })
            .IsUnique();

        entity
            .HasIndex(p => new { p.ModuleId, p.Timestamp, p.OrganizationId });

        entity
            .HasIndex(o => o.ModuleId);

        // Index for checksum queries with timestamp
        entity
            .HasIndex(o => new { o.Checksum, o.ModuleId, o.Timestamp, o.OrganizationId });

        // Foreign key relationship to Module with composite key
        entity
            .HasOne(e => e.Module)
            .WithMany(x => x.VariableSets)
            .HasForeignKey(e => new { e.ModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}