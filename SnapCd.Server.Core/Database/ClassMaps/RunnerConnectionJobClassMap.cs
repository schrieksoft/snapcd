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

/// <summary>
/// Entity configuration for RunnerConnectionJob - tracks which jobs are executing on which runner connections.
/// </summary>
public class RunnerConnectionJobClassMap : IEntityTypeConfiguration<RunnerConnectionJob>
{
    public void Configure(EntityTypeBuilder<RunnerConnectionJob> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Unique composite index on (RunnerConnectionId, ModuleJobId, OrganizationId)
        // Ensures a job can only be associated with one connection
        entity
            .HasIndex(e => new { e.RunnerConnectionId, e.ModuleJobId, e.OrganizationId })
            .IsUnique();

        // Index on RunnerConnectionId for queries
        entity.HasIndex(e => new { e.RunnerConnectionId, e.OrganizationId });

        // Index on ModuleJobId for queries
        entity.HasIndex(e => new { e.ModuleJobId, e.OrganizationId });

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // RunnerConnection navigation property with composite FK
        entity
            .HasOne(e => e.RunnerConnection)
            .WithMany()
            .HasForeignKey(e => new { e.RunnerConnectionId, e.OrganizationId })
            .HasPrincipalKey(rc => new { rc.Id, rc.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // ModuleJob navigation property with composite FK
        entity
            .HasOne(e => e.ModuleJob)
            .WithMany()
            .HasForeignKey(e => new { e.ModuleJobId, e.OrganizationId })
            .HasPrincipalKey(mj => new { mj.Id, mj.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
