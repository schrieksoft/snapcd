// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.AgentAssignments;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class AgentNamespaceAssignmentClassMap : IEntityTypeConfiguration<AgentNamespaceAssignment>
{
    public void Configure(EntityTypeBuilder<AgentNamespaceAssignment> entity)
    {
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.HasIndex(e => e.Id).IsUnique();

        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.AgentNamespaceAssignments)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasIndex(p => new { p.NamespaceId, p.AgentId, p.OrganizationId })
            .IsUnique();

        entity
            .HasIndex(p => p.NamespaceId);

        entity
            .HasIndex(p => p.AgentId);

        entity
            .HasOne(a => a.Namespace)
            .WithMany(x => x.AgentNamespaceAssignments)
            .HasForeignKey(a => new { a.NamespaceId, a.OrganizationId })
            .HasPrincipalKey(n => new { n.Id, n.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne(a => a.Agent)
            .WithMany(x => x.AgentNamespaceAssignments)
            .HasForeignKey(a => new { a.AgentId, a.OrganizationId })
            .HasPrincipalKey(ag => new { ag.Id, ag.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
