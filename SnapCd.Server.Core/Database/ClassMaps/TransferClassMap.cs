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
        entity.Property(e => e.ClosedByPrincipalDiscriminator).HasConversion<string>().HasMaxLength(50);

        entity.HasIndex(e => e.Id).IsUnique();

        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.Transfers)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict on both sides: an open Transfer is not something a Module delete may silently
        // resolve.
        entity
            .HasOne(e => e.Module)
            .WithMany(m => m.Transfers)
            .HasForeignKey(e => new { e.ModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(e => e.CounterpartyModule)
            .WithMany(m => m.CounterpartyTransfers)
            .HasForeignKey(e => new { e.CounterpartyModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TransferRunClassMap : IEntityTypeConfiguration<TransferRun>
{
    public void Configure(EntityTypeBuilder<TransferRun> entity)
    {
        entity.ToTable("TransferRuns", t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.Property(e => e.Scope).HasConversion<string>().HasMaxLength(50);

        entity.HasIndex(e => e.Id).IsUnique();
        entity.HasIndex(e => new { e.TransferId, e.OrganizationId });

        entity
            .HasOne(e => e.Transfer)
            .WithMany(t => t.Runs)
            .HasForeignKey(e => new { e.TransferId, e.OrganizationId })
            .HasPrincipalKey(t => new { t.Id, t.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class TransferObjectClassMap : IEntityTypeConfiguration<TransferObject>
{
    public void Configure(EntityTypeBuilder<TransferObject> entity)
    {
        entity.ToTable("TransferObjects", t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.Property(e => e.ResolvedByPrincipalDiscriminator).HasConversion<string>().HasMaxLength(50);

        entity.HasIndex(e => e.Id).IsUnique();

        // One row per address per transfer.
        entity.HasIndex(e => new { e.TransferId, e.Address, e.OrganizationId }).IsUnique();

        // The open ones are what every check reads.
        entity.HasIndex(e => new { e.OrganizationId, e.ArrivedModuleId, e.ArrivedAt });

        entity
            .HasOne(e => e.Transfer)
            .WithMany(t => t.Objects)
            .HasForeignKey(e => new { e.TransferId, e.OrganizationId })
            .HasPrincipalKey(t => new { t.Id, t.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity
            .HasOne(e => e.Organization)
            .WithMany()
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
