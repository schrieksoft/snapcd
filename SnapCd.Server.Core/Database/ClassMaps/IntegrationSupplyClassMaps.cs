// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Core.Entities.Definition.IntegrationSupplies;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class IntegrationModuleSupplyClassMap : IEntityTypeConfiguration<IntegrationModuleSupply>
{
    public void Configure(EntityTypeBuilder<IntegrationModuleSupply> entity)
    {
        entity.HasKey(e => new { e.Id, e.OrganizationId });
        entity.HasIndex(e => e.Id).IsUnique();
        entity.HasIndex(e => new { e.ModuleId, e.IntegrationId, e.OrganizationId }).IsUnique();
        entity.HasIndex(e => e.IntegrationId);
        entity.HasIndex(e => e.ModuleId);

        entity.HasOne(e => e.Organization).WithMany()
            .HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Integration).WithMany(i => i.ModuleAssignments)
            .HasForeignKey(e => new { e.IntegrationId, e.OrganizationId })
            .HasPrincipalKey(i => new { i.Id, i.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.Module).WithMany()
            .HasForeignKey(e => new { e.ModuleId, e.OrganizationId })
            .HasPrincipalKey(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class IntegrationNamespaceSupplyClassMap : IEntityTypeConfiguration<IntegrationNamespaceSupply>
{
    public void Configure(EntityTypeBuilder<IntegrationNamespaceSupply> entity)
    {
        entity.HasKey(e => new { e.Id, e.OrganizationId });
        entity.HasIndex(e => e.Id).IsUnique();
        entity.HasIndex(e => new { e.NamespaceId, e.IntegrationId, e.OrganizationId }).IsUnique();
        entity.HasIndex(e => e.IntegrationId);
        entity.HasIndex(e => e.NamespaceId);

        entity.HasOne(e => e.Organization).WithMany()
            .HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Integration).WithMany(i => i.NamespaceAssignments)
            .HasForeignKey(e => new { e.IntegrationId, e.OrganizationId })
            .HasPrincipalKey(i => new { i.Id, i.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.Namespace).WithMany()
            .HasForeignKey(e => new { e.NamespaceId, e.OrganizationId })
            .HasPrincipalKey(n => new { n.Id, n.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class IntegrationStackSupplyClassMap : IEntityTypeConfiguration<IntegrationStackSupply>
{
    public void Configure(EntityTypeBuilder<IntegrationStackSupply> entity)
    {
        entity.HasKey(e => new { e.Id, e.OrganizationId });
        entity.HasIndex(e => e.Id).IsUnique();
        entity.HasIndex(e => new { e.StackId, e.IntegrationId, e.OrganizationId }).IsUnique();
        entity.HasIndex(e => e.IntegrationId);
        entity.HasIndex(e => e.StackId);

        entity.HasOne(e => e.Organization).WithMany()
            .HasForeignKey(e => e.OrganizationId).OnDelete(DeleteBehavior.Restrict);

        entity.HasOne(e => e.Integration).WithMany(i => i.StackAssignments)
            .HasForeignKey(e => new { e.IntegrationId, e.OrganizationId })
            .HasPrincipalKey(i => new { i.Id, i.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        entity.HasOne(e => e.Stack).WithMany()
            .HasForeignKey(e => new { e.StackId, e.OrganizationId })
            .HasPrincipalKey(s => new { s.Id, s.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
