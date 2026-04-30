using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class OrganizationClassMap : IEntityTypeConfiguration<Organization>
{
    public void Configure(EntityTypeBuilder<Organization> entity)
    {
        entity.HasKey(e => e.Id);

        entity
            .Property(e => e.Name)
            .IsRequired()
            .HasMaxLength(127);

        entity
            .Property(e => e.Status)
            .HasConversion<string>()
            .IsRequired();

        entity
            .Property(e => e.CreatedDateTime)
            .ValueGeneratedOnAdd()
            .IsRequired();

        // Indexes
        entity
            .HasIndex(e => e.Name)
            .IsUnique();

        entity
            .HasIndex(e => e.Status);

        entity
            .HasIndex(e => e.CreatedDateTime);

        // Ignore derived type collections - use base GroupMember collection only
        entity.Ignore(e => e.UserGroupMembers);
        entity.Ignore(e => e.ServicePrincipalGroupMembers);
        entity.Ignore(e => e.GroupGroupMembers);

        entity.Ignore(e => e.GroupModuleRoleAssignments);
        entity.Ignore(e => e.GroupNamespaceRoleAssignments);
        entity.Ignore(e => e.GroupStackRoleAssignments);
        entity.Ignore(e => e.GroupOrganizationRoleAssignments);

        entity.Ignore(e => e.UserModuleRoleAssignments);
        entity.Ignore(e => e.UserNamespaceRoleAssignments);
        entity.Ignore(e => e.UserStackRoleAssignments);
        entity.Ignore(e => e.UserOrganizationRoleAssignments);

        entity.Ignore(e => e.ServicePrincipalModuleRoleAssignments);
        entity.Ignore(e => e.ServicePrincipalNamespaceRoleAssignments);
        entity.Ignore(e => e.ServicePrincipalStackRoleAssignments);
        entity.Ignore(e => e.ServicePrincipalOrganizationRoleAssignments);


        // Navigation properties
        entity
            .HasMany(e => e.OrganizationUsers)
            .WithOne(e => e.Organization)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasMany(e => e.Stacks)
            .WithOne()
            .HasForeignKey("OrganizationId")
            .OnDelete(DeleteBehavior.Restrict);
    }
}