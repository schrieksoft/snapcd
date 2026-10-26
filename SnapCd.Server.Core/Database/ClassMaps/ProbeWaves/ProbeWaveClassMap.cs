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

public class ProbeWaveClassMap : IEntityTypeConfiguration<ProbeWave>
{
    public void Configure(EntityTypeBuilder<ProbeWave> entity)
    {
        entity.ToTable("ProbeWaves", t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.HasIndex(e => e.Id).IsUnique();

        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.ProbeWaves)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // A wave belongs to the job whose approval it informs, and the Module is an attribute of
        // that job rather than a second thing the wave points at. One path, so it can cascade.
        entity
            .HasOne(e => e.OriginModuleJob)
            .WithMany()
            .HasForeignKey(e => new { e.OriginModuleJobId, e.OrganizationId })
            .HasPrincipalKey(j => new { j.Id, j.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(p => new { p.OriginModuleJobId, p.OrganizationId });
    }
}
