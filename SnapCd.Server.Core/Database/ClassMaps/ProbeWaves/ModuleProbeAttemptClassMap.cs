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

public class ModuleProbeAttemptClassMap : IEntityTypeConfiguration<ModuleProbeAttempt>
{
    public void Configure(EntityTypeBuilder<ModuleProbeAttempt> entity)
    {
        entity.ToTable("ModuleProbeAttempts", t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.HasIndex(e => e.Id).IsUnique();

        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.ModuleProbeAttempts)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(e => e.ProbeWave)
            .WithMany(x => x.Attempts)
            .HasForeignKey(e => new { e.ProbeWaveId, e.OrganizationId })
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // Restrict because the wave above already cascades and a second cascade path into this
        // table is what SQL Server refuses, not because a Module should outlive a wave that
        // mentions it. Deleting a Module has to remove its attempts itself.
        entity
            .HasOne(e => e.Module)
            .WithMany(x => x.ModuleProbeAttempts)
            .HasForeignKey(e => new { e.ModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        // One row per probe, not per Module: a probe is repeatable, so a Module can hold several
        // attempts in one wave. At most one job per Module can be current, which is enforced by a
        // filtered unique index on ModuleJobs. Timestamp is in the key because every read of an
        // attempt wants the Module's most recent one.
        entity
            .HasIndex(p => new { p.ProbeWaveId, p.ModuleId, p.OrganizationId, p.Timestamp });

        // Restrict, and it cannot be otherwise: the foreign key is composite over OrganizationId,
        // which is not nullable, and SQL Server refuses SET NULL where any referencing column is.
        // So the attempt must be deleted before the set it points at, which is what the wave's own
        // delete does by taking both.
        entity
            .HasOne(e => e.SignalOutputSet)
            .WithMany()
            .HasForeignKey(e => new { e.SignalOutputSetId, e.OrganizationId })
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(e => e.ResourceChangeSet)
            .WithMany()
            .HasForeignKey(e => new { e.ResourceChangeSetId, e.OrganizationId })
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
