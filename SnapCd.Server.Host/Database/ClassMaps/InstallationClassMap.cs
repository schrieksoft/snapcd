// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SnapCd.Server.Host.Installations;

namespace SnapCd.Server.Host.Database.ClassMaps;

public class InstallationClassMap : IEntityTypeConfiguration<Installation>
{
    public void Configure(EntityTypeBuilder<Installation> entity)
    {
        entity.HasKey(e => e.Id);
        // Single fixed-key row; the id is never generated and the database refuses any other.
        entity.Property(e => e.Id).ValueGeneratedNever();
        entity.ToTable(t => t.HasCheckConstraint("CK_Installations_Singleton", $"[Id] = {Installation.SingletonId}"));
    }
}
