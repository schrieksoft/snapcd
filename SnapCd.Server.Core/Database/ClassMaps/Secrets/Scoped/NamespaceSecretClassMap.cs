using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;

namespace SnapCd.Server.Core.Database.ClassMaps.Secrets.Scoped;

public class NamespaceSecretClassMap : IEntityTypeConfiguration<NamespaceSecret>
{
    public void Configure(EntityTypeBuilder<NamespaceSecret> entity)
    {
        // Configure the relationship to Namespace
        entity
            .HasOne(t => t.Namespace)
            .WithMany(sp => sp.SecretsScopedToNamespace)
            .HasForeignKey(store => new { store.NamespaceId, store.OrganizationId })
            .HasPrincipalKey(n => new { n.Id, n.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint on Name + NamespaceId
        entity
            .HasIndex(p => new { p.Name, p.NamespaceId })
            .IsUnique();

        // Foreign key index
        entity
            .HasIndex(p => p.NamespaceId);
    }
}