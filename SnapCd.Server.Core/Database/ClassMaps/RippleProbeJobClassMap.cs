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

public class RippleProbeJobClassMap : IEntityTypeConfiguration<RippleProbeJob>
{
    public void Configure(EntityTypeBuilder<RippleProbeJob> entity)
    {
        entity.ToTable("RippleProbeJobs", t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.HasIndex(e => e.Id).IsUnique();

        entity
            .HasOne(e => e.Module)
            .WithMany()
            .HasForeignKey(e => new { e.ModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // The upstream whose change prompted this plan. No cascade: both foreign keys point at
        // Modules, and a second cascade path to the same table is a cycle SQL Server rejects.
        entity
            .HasOne(e => e.UpstreamModule)
            .WithMany()
            .HasForeignKey(e => new { e.UpstreamModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.NoAction);

        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.RippleProbeJobs)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .Property(p => p.JobNumber)
            .UseIdentityColumn();

        entity
            .HasIndex(m => m.ModuleId);

        entity
            .HasIndex(m => new { m.ModuleId, m.TimestampStart, m.OrganizationId });

        // Every ripple probe for one upstream job, which is how the job waiting on them finds
        // them.
        entity
            .HasIndex(m => new { m.UpstreamModuleJobId, m.OrganizationId });

        // No filtered unique index on module and status, unlike StateMigrationJobs. A ripple probe
        // is a read that runs alongside the module's own work, so concurrent jobs on one module
        // are the expected case rather than the race to reject.

        entity
            .Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        entity
            .Property(d => d.FailedOnServerSideStep)
            .HasConversion<string>()
            .HasMaxLength(50);
    }
}
