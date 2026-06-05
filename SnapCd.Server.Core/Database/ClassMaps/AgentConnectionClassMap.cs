// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Database.ClassMaps;

/// <summary>
/// Entity configuration for AgentConnection - active agent connections to server instances.
/// </summary>
public class AgentConnectionClassMap : IEntityTypeConfiguration<AgentConnection>
{
    public void Configure(EntityTypeBuilder<AgentConnection> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Unique index on (OrganizationId, SignalRConnectionId)
        entity.HasIndex(e => new { e.OrganizationId, e.SignalRConnectionId }).IsUnique();

        // Unique index on (OrganizationId, AgentId, InstanceName) - only one connection per agent instance
        entity
            .HasIndex(e => new { e.OrganizationId, e.AgentId, e.InstanceName })
            .IsUnique();

        // Index on ServerInstanceId for cleanup queries
        entity.HasIndex(e => e.ServerInstanceId);

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Agent navigation property with composite FK
        entity
            .HasOne(e => e.Agent)
            .WithMany()
            .HasForeignKey(e => new { e.AgentId, e.OrganizationId })
            .HasPrincipalKey(a => new { a.Id, a.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
