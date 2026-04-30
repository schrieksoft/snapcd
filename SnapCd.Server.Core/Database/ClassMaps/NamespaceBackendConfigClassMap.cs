using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class NamespaceBackendConfigClassMap : IEntityTypeConfiguration<NamespaceBackendConfig>
{
    public void Configure(EntityTypeBuilder<NamespaceBackendConfig> builder)
    {
        builder.HasOne(x => x.Namespace)
            .WithMany(x => x.BackendConfigs)
            .HasForeignKey(x => new { x.NamespaceId, x.OrganizationId })
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.NamespaceId, x.Name })
            .IsUnique();
    }
}