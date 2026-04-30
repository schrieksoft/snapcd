using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class RunnerNamespaceAssignmentClassMap : IEntityTypeConfiguration<RunnerNamespaceAssignment>
{
    public void Configure(EntityTypeBuilder<RunnerNamespaceAssignment> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.RunnerNamespaceAssignments)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasIndex(p => new { p.NamespaceId, p.RunnerId, p.OrganizationId })
            .IsUnique();

        entity
            .HasIndex(p => p.NamespaceId);

        entity
            .HasIndex(p => p.RunnerId);

        // Configure foreign key relationship to Namespace with composite key
        entity
            .HasOne(a => a.Namespace)
            .WithMany(x => x.RunnerNamespaceAssignments)
            .HasForeignKey(a => new { a.NamespaceId, a.OrganizationId })
            .HasPrincipalKey(n => new { n.Id, n.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // Configure foreign key relationship to Runner
        entity
            .HasOne(a => a.Runner)
            .WithMany(x => x.RunnerNamespaceAssignments)
            .HasForeignKey(a => new { a.RunnerId, a.OrganizationId })
            .HasPrincipalKey(rp => new { rp.Id, rp.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}