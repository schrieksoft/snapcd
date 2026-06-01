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

public class ModuleEnvVarFromOutputClassMap : IEntityTypeConfiguration<ModuleEnvVarFromOutput>
{
    public void Configure(EntityTypeBuilder<ModuleEnvVarFromOutput> entity)
    {
        entity
            .HasIndex(m => m.ModuleId);

        entity
            .HasIndex(m => m.OutputModuleId);

        // Composite index for gatekeeping queries that filter by OutputModuleId and OutputName
        entity
            .HasIndex(m => new { m.OutputModuleId, m.OutputName });

        entity
            .HasOne(m => m.OutputModule)
            .WithMany()
            .HasForeignKey(m => new { m.OutputModuleId, m.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        // Module relationship - cascade delete when Module is deleted
        entity
            .HasOne(e => e.Module)
            .WithMany(x => x.ModuleEnvVarFromOutputs)
            .HasForeignKey(e => new { e.ModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}