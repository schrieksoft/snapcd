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

public class StackSecretClassMap : IEntityTypeConfiguration<StackSecret>
{
    public void Configure(EntityTypeBuilder<StackSecret> entity)
    {
        // Configure the relationship to Stack
        entity
            .HasOne(t => t.Stack)
            .WithMany(sp => sp.SecretsScopedToStack)
            .HasForeignKey(store => new { store.StackId, store.OrganizationId })
            .HasPrincipalKey(s => new { s.Id, s.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        // Unique constraint on Name + StackId
        entity
            .HasIndex(p => new { p.Name, p.StackId })
            .IsUnique();

        // Foreign key index
        entity
            .HasIndex(p => p.StackId);
    }
}