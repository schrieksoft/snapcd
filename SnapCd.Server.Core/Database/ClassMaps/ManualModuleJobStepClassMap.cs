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

public class ManualModuleJobStepClassMap : IEntityTypeConfiguration<ManualModuleJobStep>
{
    public void Configure(EntityTypeBuilder<ManualModuleJobStep> entity)
    {
        entity.ToTable("ManualModuleJobSteps", t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);

        entity.HasIndex(e => e.Id).IsUnique();

        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.ManualModuleJobSteps)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // One row per attempt at a task for a Module within a job.
        entity
            .HasIndex(e => new { e.JobId, e.ModuleId, e.Task, e.Attempt, e.OrganizationId })
            .IsUnique();

        entity
            .HasOne(e => e.Job)
            .WithMany(j => j.Steps)
            .HasForeignKey(e => new { e.JobId, e.OrganizationId })
            .HasPrincipalKey(j => new { j.Id, j.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // The fan-in query: every step a Transfer has run, without walking its jobs.
        entity.HasIndex(e => e.TransferId);

        entity.HasIndex(e => e.ModuleId);
    }
}
