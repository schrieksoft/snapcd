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
using SnapCd.Server.Core.Entities.Definition.ProbeWaves;

namespace SnapCd.Server.Core.Database.ClassMaps.ProbeWaves;

public class ProbeWaveParticipantClassMap : IEntityTypeConfiguration<ProbeWaveParticipant>
{
    public void Configure(EntityTypeBuilder<ProbeWaveParticipant> entity)
    {
        entity.ToTable("ProbeWaveParticipants", t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.HasIndex(e => e.Id).IsUnique();

        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.ProbeWaveParticipants)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(e => e.ProbeWave)
            .WithMany(x => x.Participants)
            .HasForeignKey(e => new { e.ProbeWaveId, e.OrganizationId })
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne(e => e.Module)
            .WithMany(x => x.ProbeWaveParticipants)
            .HasForeignKey(e => new { e.ModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        // A Module probes once per wave.
        entity
            .HasIndex(p => new { p.ProbeWaveId, p.ModuleId, p.OrganizationId })
            .IsUnique();

        entity
            .HasOne(e => e.SignalOutputSet)
            .WithMany()
            .HasForeignKey(e => new { e.SignalOutputSetId, e.OrganizationId })
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
