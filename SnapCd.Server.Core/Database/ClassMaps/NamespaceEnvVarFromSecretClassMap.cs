using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class NamespaceEnvVarFromSecretClassMap : IEntityTypeConfiguration<NamespaceEnvVarFromSecret>
{
    public void Configure(EntityTypeBuilder<NamespaceEnvVarFromSecret> entity)
    {
        entity
            .Property(d => d.Type)
            .HasConversion<string>();

        entity.HasOne(x => x.Secret)
            .WithMany()
            .HasForeignKey(x => new { x.SecretId, x.OrganizationId })
            .HasPrincipalKey(s => new { s.Id, s.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(e => e.Namespace)
            .WithMany(x => x.NamespaceEnvVarFromSecrets)
            .HasForeignKey(e => new { e.NamespaceId, e.OrganizationId })
            .HasPrincipalKey(n => new { n.Id, n.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}