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

public class ModuleEnvVarFromLiteralClassMap : IEntityTypeConfiguration<ModuleEnvVarFromLiteral>
{
    public void Configure(EntityTypeBuilder<ModuleEnvVarFromLiteral> entity)
    {
        entity
            .Property(d => d.Type)
            .HasConversion<string>();

        entity
            .HasIndex(m => m.ModuleId);

        // Module relationship - cascade delete when Module is deleted
        entity
            .HasOne(e => e.Module)
            .WithMany(x => x.ModuleEnvVarFromLiterals)
            .HasForeignKey(e => new { e.ModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}