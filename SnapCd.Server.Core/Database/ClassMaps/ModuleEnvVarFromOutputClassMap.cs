using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class ModuleEnvVarFromOutputClassMap : IEntityTypeConfiguration<ModuleEnvVarFromOutput>
{
    public void Configure(EntityTypeBuilder<ModuleEnvVarFromOutput> entity)
    {
        entity
            .HasIndex(m => m.ModuleId);

        entity
            .HasIndex(m => m.OutputModuleId);

        // Composite index for gatekeeping queries that filter by OutputModuleId and OutputName
        entity
            .HasIndex(m => new { m.OutputModuleId, m.OutputName });

        entity
            .HasOne(m => m.OutputModule)
            .WithMany()
            .HasForeignKey(m => new { m.OutputModuleId, m.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        // Module relationship - cascade delete when Module is deleted
        entity
            .HasOne(e => e.Module)
            .WithMany(x => x.ModuleEnvVarFromOutputs)
            .HasForeignKey(e => new { e.ModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}