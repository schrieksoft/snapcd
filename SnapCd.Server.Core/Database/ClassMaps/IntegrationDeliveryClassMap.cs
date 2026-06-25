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

public class IntegrationDeliveryClassMap : IEntityTypeConfiguration<IntegrationDelivery>
{
    public void Configure(EntityTypeBuilder<IntegrationDelivery> entity)
    {
        entity.HasKey(e => new { e.Id, e.OrganizationId });
        entity.HasIndex(e => e.Id).IsUnique();

        entity.Property(e => e.Trigger).HasConversion<string>().HasMaxLength(64);
        entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(32);

        // Idempotency: at most one delivery per (occurrence, subscription).
        entity.HasIndex(e => new { e.DedupeKey, e.IntegrationEventId, e.OrganizationId }).IsUnique();

        // Threading lookup: the root message for a mission on an integration.
        entity.HasIndex(e => new { e.IntegrationId, e.ModuleJobMissionId, e.OrganizationId });
        entity.HasIndex(e => e.IntegrationId);

        // Plain log: no FK to Integration (the audit row outlives the integration).
    }
}
