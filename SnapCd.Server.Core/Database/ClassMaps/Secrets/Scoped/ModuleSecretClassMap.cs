using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;

namespace SnapCd.Server.Core.Database.ClassMaps.Secrets.Scoped;

public class ModuleSecretClassMap : IEntityTypeConfiguration<ModuleSecret>
{
    public void Configure(EntityTypeBuilder<ModuleSecret> entity)
    {
        // Configure the relationship to Module
        entity
            .HasOne(t => t.Module)
            .WithMany(sp => sp.SecretsScopedToModule)
            .HasForeignKey(store => new { store.ModuleId, store.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint on Name + ModuleId
        entity
            .HasIndex(p => new { p.Name, p.ModuleId })
            .IsUnique();

        // Foreign key index
        entity
            .HasIndex(p => p.ModuleId);
    }
}