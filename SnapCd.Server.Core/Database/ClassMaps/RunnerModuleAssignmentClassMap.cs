using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class RunnerModuleAssignmentClassMap : IEntityTypeConfiguration<RunnerModuleAssignment>
{
    public void Configure(EntityTypeBuilder<RunnerModuleAssignment> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.RunnerModuleAssignments)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasIndex(p => new { p.ModuleId, p.RunnerId, p.OrganizationId })
            .IsUnique();

        entity
            .HasIndex(p => p.ModuleId);

        entity
            .HasIndex(p => p.RunnerId);

        // Configure foreign key relationship to Module with composite key
        entity
            .HasOne(a => a.Module)
            .WithMany(x => x.RunnerModuleAssignments)
            .HasForeignKey(a => new { a.ModuleId, a.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // Configure foreign key relationship to Runner
        entity
            .HasOne(a => a.Runner)
            .WithMany(x => x.RunnerModuleAssignments)
            .HasForeignKey(a => new { a.RunnerId, a.OrganizationId })
            .HasPrincipalKey(rp => new { rp.Id, rp.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}