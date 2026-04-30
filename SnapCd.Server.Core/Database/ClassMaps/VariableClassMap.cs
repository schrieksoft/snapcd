using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class VariableClassMap : IEntityTypeConfiguration<Variable>
{
    public void Configure(EntityTypeBuilder<Variable> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.Variables)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(t => t.VariableSet)
            .WithMany(sp => sp.Variables)
            .HasForeignKey(store => new { store.VariableSetId, store.OrganizationId })
            .HasPrincipalKey(sp => new { sp.Id, sp.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasIndex(p => new { p.VariableSetId, p.Name, p.OrganizationId })
            .IsUnique();

        entity
            .HasIndex(o => o.VariableSetId);
    }
}