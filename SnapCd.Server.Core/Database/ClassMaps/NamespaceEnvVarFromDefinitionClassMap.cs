using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class NamespaceEnvVarFromDefinitionClassMap : IEntityTypeConfiguration<NamespaceEnvVarFromDefinition>
{
    public void Configure(EntityTypeBuilder<NamespaceEnvVarFromDefinition> entity)
    {
        entity
            .Property(d => d.DefinitionName)
            .HasConversion<string>();

        entity
            .HasIndex(n => n.NamespaceId);


        entity
            .HasOne(e => e.Namespace)
            .WithMany(x => x.NamespaceEnvVarFromDefinitions)
            .HasForeignKey(e => new { e.NamespaceId, e.OrganizationId })
            .HasPrincipalKey(n => new { n.Id, n.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}