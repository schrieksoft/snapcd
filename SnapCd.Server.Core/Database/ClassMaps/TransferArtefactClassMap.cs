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

public class TransferArtefactClassMap : IEntityTypeConfiguration<TransferArtefact>
{
    public void Configure(EntityTypeBuilder<TransferArtefact> entity)
    {
        entity.ToTable("TransferArtefacts", t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.HasIndex(e => e.Id).IsUnique();

        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.TransferArtefacts)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // One row per transfer: a re-run of a half overwrites what it produced before.
        entity
            .HasIndex(e => new { e.TransferId, e.OrganizationId })
            .IsUnique();

        // Modelled as a collection so the organization column serves both foreign keys; the unique
        // index above is what makes it one row per transfer.
        entity
            .HasOne(e => e.Transfer)
            .WithMany(t => t.Artefacts)
            .HasForeignKey(e => new { e.TransferId, e.OrganizationId })
            .HasPrincipalKey(t => new { t.Id, t.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
