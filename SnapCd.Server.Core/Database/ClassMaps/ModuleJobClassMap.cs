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

public class ModuleJobClassMap : IEntityTypeConfiguration<ModuleJob>
{
    public ModuleJobClassMap()
    {
    }

    public void Configure(EntityTypeBuilder<ModuleJob> entity)
    {
        entity.ToTable("ModuleJobs", t => t.UseSqlOutputClause(false));

        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.ModuleJobs)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .Property(p => p.JobNumber)
            .UseIdentityColumn();

        entity
            .HasIndex(p => new { p.ModuleId, p.TimestampStart, p.OrganizationId })
            .IsUnique();

        entity
            .HasIndex(m => m.ModuleId);

        entity
            .HasIndex(m => new { m.ModuleId, m.TimestampEnd, m.OrganizationId });

        // Ensure only one ModuleJob per Module can be IsCurrent
        entity
            .HasIndex(m => new { m.ModuleId, m.IsCurrent, m.OrganizationId })
            .IsUnique()
            .HasFilter("[IsCurrent] = 1");

        // Org-wide activity feeds (dashboard Recent Activity / 7-day chart): newest-first
        // range reads without scanning the org's whole job history.
        // NOTE: both dashboard indexes share the same key columns, so they must use the
        // named HasIndex overload — otherwise EF merges them into one index definition.
        entity
            .HasIndex(m => new { m.OrganizationId, m.TimestampStart }, "IX_ModuleJobs_Organization_Activity")
            .IsDescending(false, true)
            .IncludeProperties(m => new { m.ModuleId, m.JobNumber, m.JobType, m.Status, m.WaitingForApproval, m.TimestampEnd });

        // Pending approvals (dashboard Needs Attention): near-empty filtered index
        entity
            .HasIndex(m => new { m.OrganizationId, m.TimestampStart }, "IX_ModuleJobs_PendingApprovals")
            .HasFilter("[WaitingForApproval] = 1")
            .IncludeProperties(m => new { m.ModuleId, m.JobNumber, m.JobType, m.Status });

        entity
            .Property(d => d.Status)
            .HasConversion<string>();

        entity
            .HasOne(e => e.Module)
            .WithMany(u => u.ModuleJobs)
            .HasForeignKey(e => new { e.ModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .Property(d => d.FailedOnServerSideStep)
            .HasConversion<string>();

        entity
            .Property(d => d.ActualStateHeadline)
            .HasConversion<string>();

        entity
            .Property(d => d.PolicyOutcome)
            .HasConversion<string>();
    }
}