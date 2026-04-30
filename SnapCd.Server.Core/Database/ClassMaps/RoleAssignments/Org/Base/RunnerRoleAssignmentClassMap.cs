using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Runner.Base;

namespace SnapCd.Server.Core.Database.ClassMaps.RoleAssignments.Org.Base;

public class RunnerRoleAssignmentClassMap : IEntityTypeConfiguration<RunnerRoleAssignment>
{
    public void Configure(EntityTypeBuilder<RunnerRoleAssignment> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Configure RoleAssignmentPrincipalDiscriminator to be stored as string with max length
        entity.Property(e => e.PrincipalDiscriminator)
            .HasConversion<string>()
            .HasMaxLength(32);

        // Configure TPH (Table Per Hierarchy) inheritance strategy with discriminator
        entity.HasDiscriminator(e => e.PrincipalDiscriminator)
            .HasValue<RunnerRoleAssignment>(RoleAssignmentPrincipalDiscriminator.Base) // This value won't be used, just for base
            .HasValue<UserRunnerRoleAssignment>(RoleAssignmentPrincipalDiscriminator.User)
            .HasValue<ServicePrincipalRunnerRoleAssignment>(RoleAssignmentPrincipalDiscriminator.ServicePrincipal)
            .HasValue<GroupRunnerRoleAssignment>(RoleAssignmentPrincipalDiscriminator.Group);

        // Computed column for PrincipalId based on discriminator (stored for indexing)
        entity
            .Property(x => x.PrincipalId)
            .HasComputedColumnSql(
                "CASE " +
                "WHEN [PrincipalDiscriminator] = 'User' THEN [UserId] " +
                "WHEN [PrincipalDiscriminator] = 'ServicePrincipal' THEN [ServicePrincipalId] " +
                "WHEN [PrincipalDiscriminator] = 'Group' THEN [GroupId] " +
                "END",
                true);

        // Index on PrincipalId for efficient lookups
        entity
            .HasIndex(e => e.PrincipalId);

        // Index on RunnerId for efficient lookups
        entity
            .HasIndex(e => e.RunnerId);

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.RunnerRoleAssignments)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Runner navigation property
        entity
            .HasOne(e => e.Runner)
            .WithMany(x => x.RunnerRoleAssignments)
            .HasForeignKey("RunnerId", "OrganizationId")
            .HasPrincipalKey(s => new { s.Id, s.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}