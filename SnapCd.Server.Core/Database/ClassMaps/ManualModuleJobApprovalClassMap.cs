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

public class ManualModuleJobApprovalClassMap : IEntityTypeConfiguration<ManualModuleJobApproval>
{
    public void Configure(EntityTypeBuilder<ManualModuleJobApproval> entity)
    {
        entity.ToTable("ManualModuleJobApprovals", t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.Property(e => e.PrincipalDiscriminator).HasConversion<string>().HasMaxLength(50);

        entity.HasIndex(e => e.Id).IsUnique();

        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.ManualModuleJobApprovals)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // One decision per principal per job, enforced by the database rather than by the caller.
        entity
            .HasIndex(p => new { p.ManualModuleJobId, p.PrincipalId, p.OrganizationId })
            .IsUnique();

        entity
            .HasOne(e => e.ManualModuleJob)
            .WithMany(u => u.ManualModuleJobApprovals)
            .HasForeignKey(e => new { e.ManualModuleJobId, e.OrganizationId })
            .HasPrincipalKey(u => new { u.Id, u.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(a => a.ManualModuleJobId);

        entity.HasIndex(a => a.PrincipalId);
    }
}
