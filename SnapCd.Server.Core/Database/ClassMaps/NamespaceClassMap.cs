using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class NamespaceClassMap : IEntityTypeConfiguration<Namespace>
{
    public void Configure(EntityTypeBuilder<Namespace> entity)
    {
        // Composite Primary Key
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity
            .HasIndex(p => new { p.CreatedDateTime });

        entity
            .Property(d => d.DefaultEngine)
            .HasConversion<string>()
            .HasMaxLength(50);

        entity
            .Property(d => d.TriggerBehaviourOnModified)
            .HasConversion<string>();

        entity
            .Property(p => p.CreatedDateTime)
            .ValueGeneratedOnAdd();

        // Foreign Key to Organization
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.Namespaces)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign Key to Stack (composite)
        entity
            .HasOne(e => e.Stack)
            .WithMany(e => e.Namespaces)
            .HasForeignKey(e => new { e.StackId, e.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // Updated unique index to include OrganizationId
        entity
            .HasIndex(i => new { i.OrganizationId, i.StackId, i.Name })
            .IsUnique();

        entity
            .HasIndex(n => n.StackId);

        // Navigation optimization index for reverse inherited permissions
        // Critical for Namespace -> Stack traversal performance
        entity
            .HasIndex(n => new { n.StackId, n.Id });

        // Unique index on Id field
        entity
            .HasIndex(e => e.Id)
            .IsUnique();

        // Updated relationship to Modules with composite FK
        entity
            .HasMany(e => e.Modules)
            .WithOne(e => e.Namespace)
            .HasForeignKey(e => new { e.NamespaceId, e.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity.Ignore(e => e.GroupNamespaceRoleAssignments);
        entity.Ignore(e => e.UserNamespaceRoleAssignments);
        entity.Ignore(e => e.ServicePrincipalNamespaceRoleAssignments);
    }
}