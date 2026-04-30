using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class NamespaceEnvVarFromLiteralClassMap : IEntityTypeConfiguration<NamespaceEnvVarFromLiteral>
{
    public void Configure(EntityTypeBuilder<NamespaceEnvVarFromLiteral> entity)
    {
        entity
            .Property(d => d.Type)
            .HasConversion<string>();

        entity
            .HasIndex(n => n.NamespaceId);


        entity
            .HasOne(e => e.Namespace)
            .WithMany(x => x.NamespaceEnvVarFromLiterals)
            .HasForeignKey(e => new { e.NamespaceId, e.OrganizationId })
            .HasPrincipalKey(n => new { n.Id, n.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}