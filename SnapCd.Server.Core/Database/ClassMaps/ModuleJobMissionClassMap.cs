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

public class ModuleJobMissionClassMap : IEntityTypeConfiguration<ModuleJobMission>
{
    public void Configure(EntityTypeBuilder<ModuleJobMission> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.Property(e => e.MissionType).HasConversion<string>().HasMaxLength(50);

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.ModuleJobMissions)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Dedup: one logical mission per (job, type) — collapses org- + module-scoped same-type
        // matches on the same job. Per-attempt correlation lives on ModuleJobMissionRun.
        entity.HasIndex(e => new { e.ModuleJobId, e.MissionType, e.OrganizationId }).IsUnique();

        entity
            .HasOne(e => e.ModuleJob)
            .WithMany(u => u.ModuleJobMissions)
            .HasForeignKey(e => new { e.ModuleJobId, e.OrganizationId })
            .HasPrincipalKey(u => new { u.Id, u.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // Foreign key indices
        entity.HasIndex(a => a.ModuleJobId);
        entity.HasIndex(a => a.MissionId);
        entity.HasIndex(a => a.AgentId);
    }
}
