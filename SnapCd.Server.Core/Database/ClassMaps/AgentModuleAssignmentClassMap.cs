// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.AgentAssignments;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class AgentModuleAssignmentClassMap : IEntityTypeConfiguration<AgentModuleAssignment>
{
    public void Configure(EntityTypeBuilder<AgentModuleAssignment> entity)
    {
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.HasIndex(e => e.Id).IsUnique();

        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.AgentModuleAssignments)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasIndex(p => new { p.ModuleId, p.AgentId, p.OrganizationId })
            .IsUnique();

        entity
            .HasIndex(p => p.ModuleId);

        entity
            .HasIndex(p => p.AgentId);

        entity
            .HasOne(a => a.Module)
            .WithMany(x => x.AgentModuleAssignments)
            .HasForeignKey(a => new { a.ModuleId, a.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne(a => a.Agent)
            .WithMany(x => x.AgentModuleAssignments)
            .HasForeignKey(a => new { a.AgentId, a.OrganizationId })
            .HasPrincipalKey(ag => new { ag.Id, ag.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
