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

public class StateMigrationJobApprovalClassMap : IEntityTypeConfiguration<StateMigrationJobApproval>
{
    public void Configure(EntityTypeBuilder<StateMigrationJobApproval> entity)
    {
        entity.ToTable("StateMigrationJobApprovals", t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.Property(e => e.PrincipalDiscriminator).HasConversion<string>().HasMaxLength(50);

        entity.HasIndex(e => e.Id).IsUnique();

        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.StateMigrationJobApprovals)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // One decision per principal per job, enforced by the database rather than by the caller.
        entity
            .HasIndex(p => new { p.StateMigrationJobId, p.PrincipalId, p.OrganizationId })
            .IsUnique();

        entity
            .HasOne(e => e.StateMigrationJob)
            .WithMany(u => u.StateMigrationJobApprovals)
            .HasForeignKey(e => new { e.StateMigrationJobId, e.OrganizationId })
            .HasPrincipalKey(u => new { u.Id, u.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasIndex(a => a.StateMigrationJobId);

        entity.HasIndex(a => a.PrincipalId);
    }
}
