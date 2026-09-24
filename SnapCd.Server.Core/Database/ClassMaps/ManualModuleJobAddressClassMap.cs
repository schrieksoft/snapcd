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

public class ManualModuleJobAddressClassMap : IEntityTypeConfiguration<ManualModuleJobAddress>
{
    public void Configure(EntityTypeBuilder<ManualModuleJobAddress> entity)
    {
        entity.ToTable("ManualModuleJobAddresses", t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.Property(e => e.Operation).HasConversion<string>().HasMaxLength(50);
        entity.Property(e => e.Direction).HasConversion<string>().HasMaxLength(50);
        entity.Property(e => e.Outcome).HasConversion<string>().HasMaxLength(50);

        entity.HasIndex(e => e.Id).IsUnique();

        // One row per address per direction within a job: an mv writes both halves itself.
        entity.HasIndex(e => new { e.JobId, e.Address, e.Direction, e.OrganizationId }).IsUnique();

        // "What happened to this address" across every job that touched it.
        entity.HasIndex(e => new { e.OrganizationId, e.ModuleId, e.Address });

        entity
            .HasOne(e => e.Job)
            .WithMany(j => j.Addresses)
            .HasForeignKey(e => new { e.JobId, e.OrganizationId })
            .HasPrincipalKey(j => new { j.Id, j.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
