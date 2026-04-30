using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;

namespace SnapCd.Server.Core.Database.ClassMaps.GroupMembers;

public class GroupGroupMemberClassMap : IEntityTypeConfiguration<GroupGroupMember>
{
    public void Configure(EntityTypeBuilder<GroupGroupMember> entity)
    {
        // Member Group navigation property (restrict to prevent cycles)
        entity
            .HasOne(gm => gm.MemberGroup)
            .WithMany(g => g.GroupGroupMembersAsMember)
            .HasPrincipalKey(g => new { g.Id, g.OrganizationId })
            .HasForeignKey(gm => new { gm.MemberGroupId, gm.OrganizationId })
            .OnDelete(DeleteBehavior.NoAction);

        // Unique constraint: (GroupId, MemberGroupId, OrganizationId)
        entity
            .HasIndex(gm => new { gm.GroupId, gm.MemberGroupId, gm.OrganizationId })
            .IsUnique();

        // Index on MemberGroupId for lookups
        entity
            .HasIndex(gm => gm.MemberGroupId);

        // Optimized index for reverse inherited permissions group lookup
        entity
            .HasIndex(gm => new { gm.MemberGroupId, gm.GroupId, gm.OrganizationId });
    }
}