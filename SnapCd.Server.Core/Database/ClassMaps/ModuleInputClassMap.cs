using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.Base;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class ModuleInputClassMap : IEntityTypeConfiguration<ModuleInput>
{
    public void Configure(EntityTypeBuilder<ModuleInput> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure InputKind to store as string
        entity
            .Property(e => e.InputKind)
            .HasConversion<string>();

        // Unique index on ModuleId, InputKind, and Name combination
        entity
            .HasIndex(p => new { p.ModuleId, p.InputKind, p.Name, p.OrganizationId })
            .IsUnique();

        // Foreign key index
        entity
            .HasIndex(p => p.ModuleId);
    }
}