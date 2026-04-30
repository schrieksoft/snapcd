using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;

namespace SnapCd.Server.Core.Database.ClassMaps.Secrets.Scoped;

public class StackSecretClassMap : IEntityTypeConfiguration<StackSecret>
{
    public void Configure(EntityTypeBuilder<StackSecret> entity)
    {
        // Configure the relationship to Stack
        entity
            .HasOne(t => t.Stack)
            .WithMany(sp => sp.SecretsScopedToStack)
            .HasForeignKey(store => new { store.StackId, store.OrganizationId })
            .HasPrincipalKey(s => new { s.Id, s.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint on Name + StackId
        entity
            .HasIndex(p => new { p.Name, p.StackId })
            .IsUnique();

        // Foreign key index
        entity
            .HasIndex(p => p.StackId);
    }
}