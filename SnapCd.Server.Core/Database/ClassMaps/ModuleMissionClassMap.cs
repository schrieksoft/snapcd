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

public class ModuleMissionClassMap : IEntityTypeConfiguration<ModuleMission>
{
    public void Configure(EntityTypeBuilder<ModuleMission> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.ModuleMissions)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint: one ModuleMission per (Module, Agent, MissionType)
        entity
            .HasIndex(p => new { p.ModuleId, p.AgentId, p.OrganizationId, p.MissionType })
            .IsUnique();

        entity
            .HasIndex(p => p.ModuleId);

        entity
            .HasIndex(p => p.AgentId);

        // Configure foreign key relationship to Module with composite key
        entity
            .HasOne(a => a.Module)
            .WithMany(x => x.ModuleMissions)
            .HasForeignKey(a => new { a.ModuleId, a.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // Configure foreign key relationship to Agent
        entity
            .HasOne(a => a.Agent)
            .WithMany(x => x.ModuleMissions)
            .HasForeignKey(a => new { a.AgentId, a.OrganizationId })
            .HasPrincipalKey(ag => new { ag.Id, ag.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // MissionType stored as string
        entity
            .Property(e => e.MissionType)
            .HasConversion<string>();
    }
}
