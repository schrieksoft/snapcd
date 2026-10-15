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

        entity
            .HasOne(e => e.OriginModule)
            .WithMany()
            .HasForeignKey(e => new { e.OriginModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(p => new { p.OriginModuleJobId, p.OrganizationId });
    }
}
