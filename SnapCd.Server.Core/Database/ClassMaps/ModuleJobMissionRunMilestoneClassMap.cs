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

public class ModuleJobMissionRunMilestoneClassMap : IEntityTypeConfiguration<ModuleJobMissionRunMilestone>
{
    public void Configure(EntityTypeBuilder<ModuleJobMissionRunMilestone> entity)
    {
        entity.HasKey(e => new { e.Id, e.OrganizationId });
        entity.HasIndex(e => e.Id).IsUnique();

        entity.Property(e => e.Kind).HasMaxLength(64);

        entity
            .HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // The FK on (ModuleJobMissionRunId, OrganizationId) already creates a composite index whose
        // leading column is ModuleJobMissionRunId, so a separate single-column index would be redundant.
        entity
            .HasOne<ModuleJobMissionRun>()
            .WithMany()
            .HasForeignKey(e => new { e.ModuleJobMissionRunId, e.OrganizationId })
            .HasPrincipalKey(r => new { r.Id, r.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
