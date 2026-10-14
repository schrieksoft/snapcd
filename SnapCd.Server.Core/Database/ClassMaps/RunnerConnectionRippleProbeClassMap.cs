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

/// <summary>Mirrors RunnerConnectionStateMigrationClassMap, keyed on a ripple probe instead.</summary>
public class RunnerConnectionRippleProbeClassMap : IEntityTypeConfiguration<RunnerConnectionRippleProbe>
{
    public void Configure(EntityTypeBuilder<RunnerConnectionRippleProbe> entity)
    {
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.HasIndex(e => e.Id).IsUnique();

        entity
            .HasIndex(e => new { e.RunnerConnectionId, e.RippleProbeJobId, e.OrganizationId })
            .IsUnique();

        entity.HasIndex(e => new { e.RunnerConnectionId, e.OrganizationId });
        entity.HasIndex(e => new { e.RippleProbeJobId, e.OrganizationId });

        entity
            .HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(e => e.RunnerConnection)
            .WithMany()
            .HasForeignKey(e => new { e.RunnerConnectionId, e.OrganizationId })
            .HasPrincipalKey(rc => new { rc.Id, rc.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne(e => e.RippleProbeJob)
            .WithMany()
            .HasForeignKey(e => new { e.RippleProbeJobId, e.OrganizationId })
            .HasPrincipalKey(pj => new { pj.Id, pj.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
