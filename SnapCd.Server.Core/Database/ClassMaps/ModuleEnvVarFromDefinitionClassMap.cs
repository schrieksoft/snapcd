using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class ModuleEnvVarFromDefinitionClassMap : IEntityTypeConfiguration<ModuleEnvVarFromDefinition>
{
    public void Configure(EntityTypeBuilder<ModuleEnvVarFromDefinition> entity)
    {
        entity
            .Property(d => d.DefinitionName)
            .HasConversion<string>();

        entity
            .HasIndex(m => m.ModuleId);

        // Module relationship - cascade delete when Module is deleted
        entity
            .HasOne(e => e.Module)
            .WithMany(x => x.ModuleEnvVarFromDefinitions)
            .HasForeignKey(e => new { e.ModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}