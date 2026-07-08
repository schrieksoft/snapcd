// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Views;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class RecursiveGroupMemberClassMap : IEntityTypeConfiguration<RecursiveGroupMember>
{
    public void Configure(EntityTypeBuilder<RecursiveGroupMember> entity)
    {
        entity.ToTable("RecursiveGroupMembers", t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.RootGroupId, e.RootOrganizationId, e.GroupId, e.OrganizationId });

        entity.HasIndex(e => new { e.GroupId, e.OrganizationId })
            .HasDatabaseName("IX_RGMP_GroupId_OrgId");
    }
}
