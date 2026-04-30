using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class ModuleJobApprovalClassMap : IEntityTypeConfiguration<ModuleJobApproval>
{
    public void Configure(EntityTypeBuilder<ModuleJobApproval> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.ModuleJobApprovals)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasIndex(p => new { p.ModuleJobId, p.PrincipalId, p.OrganizationId })
            .IsUnique();

        entity
            .HasOne(e => e.ModuleJob)
            .WithMany(u => u.ModuleJobApprovals)
            .HasForeignKey(e => new { e.ModuleJobId, e.OrganizationId })
            .HasPrincipalKey(u => new { u.Id, u.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key indices
        entity
            .HasIndex(a => a.ModuleJobId);

        entity
            .HasIndex(a => a.PrincipalId);
    }
}