using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class ModuleEnvVarFromSecretClassMap : IEntityTypeConfiguration<ModuleEnvVarFromSecret>
{
    public void Configure(EntityTypeBuilder<ModuleEnvVarFromSecret> entity)
    {
        entity
            .Property(d => d.Type)
            .HasConversion<string>();

        entity
            .HasIndex(m => m.ModuleId);

        entity
            .HasOne(m => m.Secret)
            .WithMany()
            .HasForeignKey(m => new { m.SecretId, m.OrganizationId })
            .HasPrincipalKey(s => new { s.Id, s.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasIndex(m => m.SecretId);

        // Module relationship - cascade delete when Module is deleted
        entity
            .HasOne(e => e.Module)
            .WithMany(x => x.ModuleEnvVarFromSecrets)
            .HasForeignKey(e => new { e.ModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}