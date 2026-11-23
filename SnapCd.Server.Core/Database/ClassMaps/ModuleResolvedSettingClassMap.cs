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
using SnapCd.Server.Core.Views;

namespace SnapCd.Server.Core.Database.ClassMaps;

public class ModuleResolvedSettingClassMap : IEntityTypeConfiguration<ModuleResolvedSetting>
{
    public void Configure(EntityTypeBuilder<ModuleResolvedSetting> entity)
    {
        entity.ToTable("ModuleResolvedSettings", t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.ModuleId, e.OrganizationId });

        entity
            .HasOne<Module>()
            .WithOne(m => m.Resolved)
            .HasForeignKey<ModuleResolvedSetting>(e => new { e.ModuleId, e.OrganizationId })
            .HasPrincipalKey<Module>(m => new { m.Id, m.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);

        // NoAction: the Namespace is at or above the Module's own, and deleting it deletes the Module.
        entity
            .HasOne<Namespace>()
            .WithMany()
            .HasForeignKey(e => new { e.OutputRestrictionExemptionNamespaceId, e.OrganizationId })
            .HasPrincipalKey(n => new { n.Id, n.OrganizationId })
            .OnDelete(DeleteBehavior.NoAction);

        entity.Property(e => e.Engine).HasConversion<string>().HasMaxLength(50);
        entity.Property(e => e.OutputSetDownstreamRestriction).HasConversion<string>().HasMaxLength(50);
        entity.Property(e => e.OutputRestrictionExemption).HasConversion<string>().HasMaxLength(50);
    }
}
