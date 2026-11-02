// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.OutputSupplies;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class OutputModuleSupplyClassMap : IEntityTypeConfiguration<OutputModuleSupply>
{
    public void Configure(EntityTypeBuilder<OutputModuleSupply> entity)
    {
        // Composite primary key with OrganizationId
        entity.HasKey(e => new { e.Id, e.OrganizationId });

        // Unique index on Id
        entity.HasIndex(e => e.Id).IsUnique();

        // Organization navigation property
        entity
            .HasOne(e => e.Organization)
            .WithMany(x => x.OutputModuleSupplies)
            .HasForeignKey(e => e.OrganizationId)
            .OnDelete(DeleteBehavior.Restrict);

        entity
            .HasIndex(p => new { p.ModuleId, p.OutputName, p.ConsumingModuleId, p.OrganizationId })
            .IsUnique();

        entity
            .HasIndex(p => p.ModuleId);

        entity
            .HasIndex(p => p.ConsumingModuleId);

        entity
            .HasOne(a => a.Module)
            .WithMany(x => x.SuppliedOutputs)
            .HasForeignKey(a => new { a.ModuleId, a.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // SQL Server allows one cascade path into a table, and the producer holds it.
        // ModuleRepository clears these rows when the consuming Module is deleted.
        entity
            .HasOne(a => a.ConsumingModule)
            .WithMany(x => x.ReceivedOutputSupplies)
            .HasForeignKey(a => new { a.ConsumingModuleId, a.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}
