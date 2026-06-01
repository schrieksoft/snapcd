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

public class ServicePrincipalGroupMemberClassMap : IEntityTypeConfiguration<ServicePrincipalGroupMember>
{
    public void Configure(EntityTypeBuilder<ServicePrincipalGroupMember> entity)
    {
        // ServicePrincipal navigation property
        entity
            .HasOne(e => e.ServicePrincipal)
            .WithMany(x => x.ServicePrincipalGroupMembers)
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .HasForeignKey(e => new { e.ServicePrincipalId, e.OrganizationId })
            .OnDelete(DeleteBehavior.NoAction);

        // Unique constraint: (GroupId, ServicePrincipalId, OrganizationId)
        entity
            .HasIndex(gm => new { gm.GroupId, gm.ServicePrincipalId, gm.OrganizationId })
            .IsUnique();

        // Index on ServicePrincipalId for lookups
        entity
            .HasIndex(gm => gm.ServicePrincipalId);

        // Optimized index for reverse inherited permissions group lookup
        entity
            .HasIndex(gm => new { gm.ServicePrincipalId, gm.GroupId, gm.OrganizationId });
    }
}