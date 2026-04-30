using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class JobRunnerAssignmentClassMap : IEntityTypeConfiguration<JobRunnerAssignment>
{
    public void Configure(EntityTypeBuilder<JobRunnerAssignment> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Unique index on JobId - each job can only be assigned to one runner
        entity.HasIndex(e => e.JobId)
            .IsUnique()
            .HasDatabaseName("IX_JobRunnerAssignment_JobId");

        // Index on RunnerIdentityId for finding all jobs assigned to a runner
        entity.HasIndex(e => e.RunnerIdentityId)
            .HasDatabaseName("IX_JobRunnerAssignment_RunnerIdentityId");

        // Index on Status for filtering by assignment status
        entity.HasIndex(e => e.Status)
            .HasDatabaseName("IX_JobRunnerAssignment_Status");

        // Composite index on RunnerIdentityId and Status for common queries
        entity.HasIndex(e => new { e.RunnerIdentityId, e.Status })
            .HasDatabaseName("IX_JobRunnerAssignment_RunnerIdentityId_Status");

        // Foreign key to Organization
        entity.HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign key to ModuleJob (composite key with OrganizationId)
        entity.HasOne(e => e.Job)
            .WithMany()
            .HasForeignKey(e => new { e.JobId, e.OrganizationId })
            .HasPrincipalKey(j => new { j.Id, j.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}