// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;

namespace SnapCd.Server.Core.Database.ClassMaps.GroupMembers;

public class UserGroupMemberClassMap : IEntityTypeConfiguration<UserGroupMember>
{
    public void Configure(EntityTypeBuilder<UserGroupMember> entity)
    {
        // OrganizationUser navigation property - FK to (UserId, OrganizationId)
        entity
            .HasOne(e => e.OrganizationUser)
            .WithMany(x => x.UserGroupMembers)
            .HasPrincipalKey(x => new { x.UserId, x.OrganizationId })
            .HasForeignKey(e => new { e.UserId, e.OrganizationId })
            .OnDelete(DeleteBehavior.NoAction);

        // Unique constraint: (GroupId, UserId, OrganizationId)
        entity
            .HasIndex(gm => new { gm.GroupId, gm.UserId, gm.OrganizationId })
            .IsUnique();

        // Index on UserId for lookups
        entity
            .HasIndex(gm => gm.UserId);

        // Optimized index for reverse inherited permissions group lookup
        entity
            .HasIndex(gm => new { gm.UserId, gm.GroupId, gm.OrganizationId });
    }
}