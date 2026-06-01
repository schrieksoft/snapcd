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

public class NamespaceHookClassMap : IEntityTypeConfiguration<NamespaceHook>
{
    public void Configure(EntityTypeBuilder<NamespaceHook> builder)
    {
        builder.Property(x => x.Task)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(x => x.Phase)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.HasOne(x => x.Namespace)
            .WithMany(x => x.Hooks)
            .HasForeignKey(x => new { x.NamespaceId, x.OrganizationId })
            .HasPrincipalKey(x => new { x.Id, x.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => new { x.NamespaceId, x.Task, x.Phase })
            .IsUnique();
    }
}
