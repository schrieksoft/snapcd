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

public class NamespaceEnvVarFromLiteralClassMap : IEntityTypeConfiguration<NamespaceEnvVarFromLiteral>
{
    public void Configure(EntityTypeBuilder<NamespaceEnvVarFromLiteral> entity)
    {
        entity
            .Property(d => d.Type)
            .HasConversion<string>();

        entity
            .HasIndex(n => n.NamespaceId);


        entity
            .HasOne(e => e.Namespace)
            .WithMany(x => x.NamespaceEnvVarFromLiterals)
            .HasForeignKey(e => new { e.NamespaceId, e.OrganizationId })
            .HasPrincipalKey(n => new { n.Id, n.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}