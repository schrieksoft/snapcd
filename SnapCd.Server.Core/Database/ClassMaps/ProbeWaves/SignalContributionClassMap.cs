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

public class SignalContributionClassMap : IEntityTypeConfiguration<SignalContribution>
{
    public void Configure(EntityTypeBuilder<SignalContribution> entity)
    {
        entity.ToTable("SignalContributions", t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.HasIndex(e => e.Id).IsUnique();

        entity
            .Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.SignalContributions)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(e => e.ProbeWaveParticipant)
            .WithMany(x => x.Contributions)
            .HasForeignKey(e => new { e.ProbeWaveParticipantId, e.OrganizationId })
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // The upstream side is Restrict: both ends are participants of one wave, and two cascade
        // paths into the same table is what SQL Server refuses.
        entity
            .HasOne(e => e.UpstreamParticipant)
            .WithMany()
            .HasForeignKey(e => new { e.UpstreamParticipantId, e.OrganizationId })
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        // One edge per consumer and upstream pair.
        entity
            .HasIndex(p => new { p.ProbeWaveParticipantId, p.UpstreamParticipantId, p.OrganizationId })
            .IsUnique();

        // The release check reads a participant's contributions by status.
        entity
            .HasIndex(p => new { p.ProbeWaveParticipantId, p.Status, p.OrganizationId });
    }
}
