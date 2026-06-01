// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;

namespace SnapCd.Server.Core.Database.ClassMaps.Secrets.Scoped;

public class ModuleSecretClassMap : IEntityTypeConfiguration<ModuleSecret>
{
    public void Configure(EntityTypeBuilder<ModuleSecret> entity)
    {
        // Configure the relationship to Module
        entity
            .HasOne(t => t.Module)
            .WithMany(sp => sp.SecretsScopedToModule)
            .HasForeignKey(store => new { store.ModuleId, store.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint on Name + ModuleId
        entity
            .HasIndex(p => new { p.Name, p.ModuleId })
            .IsUnique();

        // Foreign key index
        entity
            .HasIndex(p => p.ModuleId);
    }
}