// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.ProbeWaves;
using SnapCd.Server.Core.Entities.Definition.Signals;

namespace SnapCd.Server.Core.Database.ClassMaps.Signals;

public class ResourceChangeSetClassMap : IEntityTypeConfiguration<ResourceChangeSet>
{
    public void Configure(EntityTypeBuilder<ResourceChangeSet> entity)
    {
        entity.ToTable("ResourceChangeSets", t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.HasIndex(e => e.Id).IsUnique();

        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.ResourceChangeSets)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // The wave owns the set, as it does a SignalOutputSet.
        entity
            .HasOne(e => e.ProbeWave)
            .WithMany(x => x.ResourceChangeSets)
            .HasForeignKey(e => new { e.ProbeWaveId, e.OrganizationId })
            .HasPrincipalKey(w => new { w.Id, w.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict for the same reason as SignalOutputSets: two cascade paths from the Module.
        entity
            .HasOne(e => e.Module)
            .WithMany(x => x.ResourceChangeSets)
            .HasForeignKey(e => new { e.ModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasIndex(p => new { p.ModuleId, p.Timestamp, p.OrganizationId });
    }
}
