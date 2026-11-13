// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Views;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class NamespaceAncestorClassMap : IEntityTypeConfiguration<NamespaceAncestor>
{
    public void Configure(EntityTypeBuilder<NamespaceAncestor> entity)
    {
        entity.ToTable("NamespaceAncestors", t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.NamespaceId, e.AncestorNamespaceId, e.OrganizationId });

        // Resolution reads a Namespace's ancestors; invalidation reads an ancestor's descendants.
        entity.HasIndex(e => new { e.AncestorNamespaceId, e.OrganizationId })
            .HasDatabaseName("IX_NsAnc_AncestorId_OrgId");
    }
}
