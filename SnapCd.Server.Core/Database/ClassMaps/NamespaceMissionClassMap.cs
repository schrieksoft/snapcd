// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.Missions;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class NamespaceMissionClassMap : IEntityTypeConfiguration<NamespaceMission>
{
    public void Configure(EntityTypeBuilder<NamespaceMission> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.NamespaceMissions)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: one NamespaceMission per (Namespace, Agent, MissionType)
        entity
            .HasIndex(p => new { p.NamespaceId, p.AgentId, p.OrganizationId, p.MissionType })
            .IsUnique();

        entity
            .HasIndex(p => p.NamespaceId);

        entity
            .HasIndex(p => p.AgentId);

        // Configure foreign key relationship to Namespace with composite key
        entity
            .HasOne(a => a.Namespace)
            .WithMany(x => x.NamespaceMissions)
            .HasForeignKey(a => new { a.NamespaceId, a.OrganizationId })
            .HasPrincipalKey(n => new { n.Id, n.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // Configure foreign key relationship to Agent
        entity
            .HasOne(a => a.Agent)
            .WithMany(x => x.NamespaceMissions)
            .HasForeignKey(a => new { a.AgentId, a.OrganizationId })
            .HasPrincipalKey(ag => new { ag.Id, ag.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // MissionType stored as string
        entity
            .Property(e => e.MissionType)
            .HasConversion<string>();
    }
}
