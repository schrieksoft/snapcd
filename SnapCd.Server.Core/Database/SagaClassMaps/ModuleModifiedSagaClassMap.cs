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
using SnapCd.Server.Core.Entities.Sagas;

namespace SnapCd.Server.Core.Database.SagaClassMaps;

public class ModuleModifiedSagaClassMap : SagaClassMap<ModuleModifiedSaga>
{
    public ModuleModifiedSagaClassMap()
    {
    }

    protected override void Configure(EntityTypeBuilder<ModuleModifiedSaga> entity, ModelBuilder modelBuilder)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.CorrelationId, e.OrganizationId });

        // Unique index on CorrelationId
        entity.HasIndex(e => e.CorrelationId).IsUnique();

        entity.Property(x => x.CurrentState).HasMaxLength(64);

        entity.Property(x => x.RowVersion).IsRowVersion(); // for optimistic concurrency
    }
}