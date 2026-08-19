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

public class ManualModuleJobClassMap : IEntityTypeConfiguration<ManualModuleJob>
{
    public void Configure(EntityTypeBuilder<ManualModuleJob> entity)
    {
        entity.ToTable("ManualModuleJobs", t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.HasIndex(e => e.Id).IsUnique();

        entity
            .HasOne(e => e.Module)
            .WithMany()
            .HasForeignKey(e => new { e.ModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.ManualModuleJobs)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .Property(p => p.JobNumber)
            .UseIdentityColumn();

        entity
            .HasIndex(m => m.ModuleId);

        entity
            .HasIndex(m => new { m.ModuleId, m.TimestampStart, m.OrganizationId });

        // At most one unfinished manual job per module. There is no gatekeeping saga serialising
        // these requests, so two concurrent launches would both pass an application-level check;
        // the second insert hits this index and throws instead. SQL-Server filtered index.
        entity
            .HasIndex(e => new { e.ModuleId, e.OrganizationId })
            .IsUnique()
            .HasFilter("[Status] = 'Running'");

        entity
            .Property(d => d.Status)
            .HasConversion<string>()
            .HasMaxLength(50);
    }
}
