using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class ModuleBackendConfigClassMap : IEntityTypeConfiguration<ModuleBackendConfig>
{
    public void Configure(EntityTypeBuilder<ModuleBackendConfig> builder)
    {
        builder.HasOne(x => x.Module)
            .WithMany(x => x.BackendConfigs)
            .HasForeignKey(x => new { x.ModuleId, x.OrganizationId })
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ModuleId, x.Name })
            .IsUnique();
    }
}