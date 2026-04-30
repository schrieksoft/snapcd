using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.Secrets;

namespace SnapCd.Server.Core.Database.ClassMaps.Secrets;

public class SecretClassMap : IEntityTypeConfiguration<Secret>
{
    public void Configure(EntityTypeBuilder<Secret> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.Secrets)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure auto-include for SecretStore navigation property
        // entity.Navigation(s => s.Organization).AutoInclude();

        // Configure ScopeKind to store as string
        entity
            .Property(e => e.ScopeKind)
            .HasConversion<string>();
    }
}