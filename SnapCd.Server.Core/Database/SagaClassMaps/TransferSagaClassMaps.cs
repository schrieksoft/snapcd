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

public class TransferSagaClassMap : SagaClassMap<TransferSaga>
{
    protected override void Configure(EntityTypeBuilder<TransferSaga> entity, ModelBuilder modelBuilder)
    {
        entity.ToTable("TransferSagas", t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.CorrelationId, e.OrganizationId });

        entity.HasIndex(e => e.CorrelationId).IsUnique();

        entity.Property(x => x.CurrentState).HasMaxLength(64);

        // One coordinator per Transfer, for its whole life.
        entity.HasIndex(e => new { e.TransferId, e.OrganizationId }).IsUnique();

        entity.Property(x => x.RowVersion).IsRowVersion();
    }
}

public class TransferParticipantSagaClassMap : SagaClassMap<TransferParticipantSaga>
{
    protected override void Configure(EntityTypeBuilder<TransferParticipantSaga> entity, ModelBuilder modelBuilder)
    {
        entity.ToTable("TransferParticipantSagas", t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.CorrelationId, e.OrganizationId });

        entity.HasIndex(e => e.CorrelationId).IsUnique();

        entity.Property(x => x.CurrentState).HasMaxLength(64);
        entity.Property(x => x.Role).HasConversion<string>().HasMaxLength(50);

        // One saga per side per Transfer, for the Transfer's whole life.
        entity.HasIndex(e => new { e.TransferId, e.Role, e.OrganizationId }).IsUnique();

        entity
            .HasOne<Module>()
            .WithMany(m => m.TransferParticipantSagas)
            .HasForeignKey(s => new { s.ModuleId, s.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity.Property(x => x.RowVersion).IsRowVersion();
    }
}
