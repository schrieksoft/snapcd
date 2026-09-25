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

public class TransferClassMap : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> entity)
    {
        entity.ToTable("Transfers", t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.Property(e => e.ConsentStatus).HasConversion<string>().HasMaxLength(50);
        entity.Property(e => e.ConsentPrincipalDiscriminator).HasConversion<string>().HasMaxLength(50);

        entity.HasIndex(e => e.Id).IsUnique();

        // The open transfer a Module's page reads, in either role.
        entity.HasIndex(e => new { e.OrganizationId, e.ModuleId, e.ClosedAt });
        entity.HasIndex(e => new { e.OrganizationId, e.CounterpartyModuleId, e.ClosedAt });

        // Restrict on both sides: an open transfer is not something a Module delete may silently
        // resolve.
        entity
            .HasOne(e => e.Module)
            .WithMany()
            .HasForeignKey(e => new { e.ModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(e => e.CounterpartyModule)
            .WithMany()
            .HasForeignKey(e => new { e.CounterpartyModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TransferLockClassMap : IEntityTypeConfiguration<TransferLock>
{
    public void Configure(EntityTypeBuilder<TransferLock> entity)
    {
        entity.ToTable("TransferLocks", t => t.UseSqlOutputClause(false));

        // The whole point of the table: one row per Module across every open transfer.
        entity.HasKey(e => new { e.OrganizationId, e.ModuleId });

        entity
            .HasOne(e => e.Transfer)
            .WithMany()
            .HasForeignKey(e => new { e.TransferId, e.OrganizationId })
            .HasPrincipalKey(t => new { t.Id, t.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
