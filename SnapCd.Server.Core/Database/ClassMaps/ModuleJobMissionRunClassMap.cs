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

public class ModuleJobMissionRunClassMap : IEntityTypeConfiguration<ModuleJobMissionRun>
{
    public void Configure(EntityTypeBuilder<ModuleJobMissionRun> entity)
    {
        entity.HasKey(e => new { e.Id, e.OrganizationId });
        entity.HasIndex(e => e.Id).IsUnique();

        entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);
        entity.Property(e => e.DiagnosisCategory).HasConversion<string>().HasMaxLength(32);

        // Per-attempt correlation id is globally unique.
        entity.HasIndex(e => e.InvocationId).IsUnique();

        // THE HARD LOCK: at most one *active* (non-terminal) run per (job, type). A second claim
        // racing on a different instance hits this and the insert throws DbUpdateException — the
        // database, not application logic, guarantees a single run. SQL-Server filtered index.
        entity
            .HasIndex(e => new { e.ModuleJobId, e.MissionType, e.OrganizationId })
            .IsUnique()
            .HasFilter("[Status] IN ('Pending', 'Running', 'AwaitingReconnect')");

        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.ModuleJobMissionRuns)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(e => e.ModuleJobMission)
            .WithMany(m => m.Runs)
            .HasForeignKey(e => new { e.ModuleJobMissionId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(a => a.ModuleJobMissionId);
        entity.HasIndex(a => a.ModuleJobId);
        entity.HasIndex(a => a.AgentId);
        entity.HasIndex(a => a.Status);
    }
}
