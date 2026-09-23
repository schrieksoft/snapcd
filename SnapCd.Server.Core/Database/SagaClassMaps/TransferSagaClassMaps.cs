// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Sagas;

namespace SnapCd.Server.Core.Database.SagaClassMaps;

public class TransferMigrateSagaClassMap : SagaClassMap<TransferMigrateSaga>
{
    protected override void Configure(EntityTypeBuilder<TransferMigrateSaga> entity, ModelBuilder modelBuilder)
    {
        entity.ToTable("TransferMigrateSagas", t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.CorrelationId, e.OrganizationId });

        entity.HasIndex(e => e.CorrelationId).IsUnique();

        entity.Property(x => x.CurrentState).HasMaxLength(64);
        entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(50);

        // One job per Module per transfer. Finishing a side that failed is a new transfer, not a
        // second job on this one, so the pair is unique.
        entity.HasIndex(e => new { e.TransferId, e.Role, e.OrganizationId }).IsUnique();

        entity
            .HasOne<Module>()
            .WithMany()
            .HasForeignKey(s => new { s.ModuleId, s.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity.Property(x => x.RowVersion).IsRowVersion();
    }
}
