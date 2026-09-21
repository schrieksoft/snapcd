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

        entity.Property(e => e.Status).HasConversion<string>().HasMaxLength(50);
        entity.Property(e => e.ReceiverConsentStatus).HasConversion<string>().HasMaxLength(50);
        entity.Property(e => e.ReceiverConsentPrincipalDiscriminator).HasConversion<string>().HasMaxLength(50);
        entity.Property(e => e.SourceLockedByPrincipalDiscriminator).HasConversion<string>().HasMaxLength(50);
        entity.Property(e => e.SourceMergedDeclaredByPrincipalDiscriminator).HasConversion<string>().HasMaxLength(50);
        entity.Property(e => e.ReceiverLockedByPrincipalDiscriminator).HasConversion<string>().HasMaxLength(50);
        entity.Property(e => e.ReceiverMergedDeclaredByPrincipalDiscriminator).HasConversion<string>().HasMaxLength(50);

        entity.HasIndex(e => e.Id).IsUnique();

        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.Transfers)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Restrict on both sides: a Transfer holding a Module still is not something a Module
        // delete may silently resolve.
        entity
            .HasOne(e => e.SourceModule)
            .WithMany(m => m.TransfersAsSource)
            .HasForeignKey(e => new { e.SourceModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasOne(e => e.ReceiverModule)
            .WithMany(m => m.TransfersAsReceiver)
            .HasForeignKey(e => new { e.ReceiverModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);

        entity.HasIndex(e => e.SourceModuleId);
        entity.HasIndex(e => e.ReceiverModuleId);
        entity.HasIndex(e => e.MapHash);
    }
}
