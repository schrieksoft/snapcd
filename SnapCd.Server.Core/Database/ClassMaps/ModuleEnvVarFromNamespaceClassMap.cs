using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class ModuleEnvVarFromNamespaceClassMap : IEntityTypeConfiguration<ModuleEnvVarFromNamespace>
{
    public void Configure(EntityTypeBuilder<ModuleEnvVarFromNamespace> entity)
    {
        entity
            .HasIndex(m => m.ModuleId);

        entity
            .HasIndex(m => m.NamespaceInputId);

        // Module relationship - cascade delete when Module is deleted
        entity
            .HasOne(e => e.Module)
            .WithMany(x => x.ModuleEnvVarFromNamespaces)
            .HasForeignKey(e => new { e.ModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}