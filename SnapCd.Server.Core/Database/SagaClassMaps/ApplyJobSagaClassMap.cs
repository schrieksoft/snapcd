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

public class ApplyJobSagaClassMap : SagaClassMap<ApplyJobSaga>
{
    public ApplyJobSagaClassMap()
    {
    }

    protected override void Configure(EntityTypeBuilder<ApplyJobSaga> entity, ModelBuilder modelBuilder)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.CorrelationId, e.OrganizationId });

        // Unique index on CorrelationId
        entity.HasIndex(e => e.CorrelationId).IsUnique();

        entity.Property(x => x.CurrentState).HasMaxLength(64);

        // Configure FK relationship to Module
        var moduleEntity = modelBuilder.Entity<Module>();
        entity
            .HasOne<Module>()
            .WithMany(m => m.ApplyModuleSaga)
            .HasForeignKey(s => new { s.ModuleId, s.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity.Property(x => x.RowVersion).IsRowVersion();
        
        entity
            .Property(d => d.CurrentState)
            .HasConversion<string>();
    }
}