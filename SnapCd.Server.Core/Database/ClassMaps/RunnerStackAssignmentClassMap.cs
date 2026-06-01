// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.RunnerAssignments;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class RunnerStackAssignmentClassMap : IEntityTypeConfiguration<RunnerStackAssignment>
{
    public void Configure(EntityTypeBuilder<RunnerStackAssignment> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.RunnerStackAssignments)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasIndex(p => new { p.StackId, p.RunnerId, p.OrganizationId })
            .IsUnique();

        entity
            .HasIndex(p => p.StackId);

        entity
            .HasIndex(p => p.RunnerId);

        // Configure foreign key relationship to Stack with composite key
        entity
            .HasOne(a => a.Stack)
            .WithMany(x => x.RunnerStackAssignments)
            .HasForeignKey(a => new { a.StackId, a.OrganizationId })
            .HasPrincipalKey(s => new { s.Id, s.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // Configure foreign key relationship to Runner
        entity
            .HasOne(a => a.Runner)
            .WithMany(x => x.RunnerStackAssignments)
            .HasForeignKey(a => new { a.RunnerId, a.OrganizationId })
            .HasPrincipalKey(rp => new { rp.Id, rp.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}