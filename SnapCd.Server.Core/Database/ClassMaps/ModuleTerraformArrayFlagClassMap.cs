using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class ModuleTerraformArrayFlagClassMap : IEntityTypeConfiguration<ModuleTerraformArrayFlag>
{
    public void Configure(EntityTypeBuilder<ModuleTerraformArrayFlag> builder)
    {
        builder.Property(x => x.Task)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Flag)
            .HasConversion<string>()
            .HasMaxLength(100);

        builder.HasOne(x => x.Module)
            .WithMany(x => x.TerraformArrayFlags)
            .HasForeignKey(x => new { x.ModuleId, x.OrganizationId })
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.ModuleId, x.Task, x.Flag, x.Value })
            .IsUnique();
    }
}
