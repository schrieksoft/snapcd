using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class ModuleParamFromOutputSetClassMap : IEntityTypeConfiguration<ModuleParamFromOutputSet>
{
    public void Configure(EntityTypeBuilder<ModuleParamFromOutputSet> entity)
    {
        entity
            .HasIndex(m => m.ModuleId);

        entity
            .HasIndex(m => m.OutputModuleId);

        entity
            .HasOne(m => m.OutputModule)
            .WithMany()
            .HasForeignKey(m => new { m.OutputModuleId, m.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        // Module relationship - cascade delete when Module is deleted
        entity
            .HasOne(e => e.Module)
            .WithMany(x => x.ModuleParamFromOutputSets)
            .HasForeignKey(e => new { e.ModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}