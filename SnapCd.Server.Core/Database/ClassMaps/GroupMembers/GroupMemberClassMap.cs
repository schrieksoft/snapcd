// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;

namespace SnapCd.Server.Core.Database.ClassMaps.GroupMembers;

public class GroupMemberClassMap : IEntityTypeConfiguration<GroupMember>
{
    public void Configure(EntityTypeBuilder<GroupMember> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Configure GroupMemberDiscriminator to be stored as string with max length
        entity.Property(e => e.GroupMemberDiscriminator)
            .HasConversion<string>()
            .HasMaxLength(32);

        // Configure TPH (Table Per Hierarchy) inheritance strategy with discriminator
        entity.HasDiscriminator(e => e.GroupMemberDiscriminator)
            .HasValue<GroupMember>(GroupMemberDiscriminator.Base)
            .HasValue<UserGroupMember>(GroupMemberDiscriminator.User)
            .HasValue<ServicePrincipalGroupMember>(GroupMemberDiscriminator.ServicePrincipal)
            .HasValue<GroupGroupMember>(GroupMemberDiscriminator.Group);

        // Computed column for PrincipalId based on discriminator (stored for indexing)
        entity
            .Property(x => x.PrincipalId)
            .HasComputedColumnSql(
                "CASE " +
                "WHEN [GroupMemberDiscriminator] = 'User' THEN [UserId] " +
                "WHEN [GroupMemberDiscriminator] = 'ServicePrincipal' THEN [ServicePrincipalId] " +
                "WHEN [GroupMemberDiscriminator] = 'Group' THEN [MemberGroupId] " +
                "END",
                true);

        // Index on PrincipalId for efficient lookups
        entity
            .HasIndex(e => e.PrincipalId);

        // Parent Group navigation property
        entity
            .HasOne(gm => gm.Group)
            .WithMany(g => g.GroupMembers)
            .HasForeignKey(gm => new { gm.GroupId, gm.OrganizationId })
            .HasPrincipalKey(g => new { g.Id, g.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.GroupMembers)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}