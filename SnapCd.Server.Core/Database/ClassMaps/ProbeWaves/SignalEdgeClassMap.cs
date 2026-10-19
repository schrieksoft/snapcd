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

namespace SnapCd.Server.Core.Database.ClassMaps.ProbeWaves;

public class SignalEdgeClassMap : IEntityTypeConfiguration<SignalEdge>
{
    public void Configure(EntityTypeBuilder<SignalEdge> entity)
    {
        entity.ToTable("SignalEdges", t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.HasIndex(e => e.Id).IsUnique();

        entity
            .Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.SignalEdges)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(e => e.ProbeWave)
            .WithMany(x => x.Contributions)
            .HasForeignKey(e => new { e.ProbeWaveId, e.OrganizationId })
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // Both Module ends are Restrict because two cascade paths into one table is what SQL
        // Server refuses, not because a Module should survive a wave that mentions it. Deleting a
        // Module has to remove its edges itself.
        entity
            .HasOne(e => e.ConsumerModule)
            .WithMany()
            .HasForeignKey(e => new { e.ConsumerModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(e => e.UpstreamModule)
            .WithMany()
            .HasForeignKey(e => new { e.UpstreamModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        // One edge per consumer and upstream pair in a wave. Two upstreams reaching one consumer
        // write two rows that share nothing, which is what keeps concurrent writers apart.
        entity
            .HasIndex(p => new { p.ProbeWaveId, p.ConsumerModuleId, p.UpstreamModuleId, p.OrganizationId })
            .IsUnique();

        // The release check reads one consumer's edges by status.
        entity
            .HasIndex(p => new { p.ProbeWaveId, p.ConsumerModuleId, p.Status, p.OrganizationId });
    }
}
