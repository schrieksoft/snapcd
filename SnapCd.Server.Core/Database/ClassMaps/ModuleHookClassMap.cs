using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class ModuleHookClassMap : IEntityTypeConfiguration<ModuleHook>
{
    public void Configure(EntityTypeBuilder<ModuleHook> builder)
    {
        builder.Property(x => x.Task)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Phase)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(x => x.Module)
            .WithMany(x => x.Hooks)
            .HasForeignKey(x => new { x.ModuleId, x.OrganizationId })
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ModuleId, x.Task, x.Phase })
            .IsUnique();
    }
}
