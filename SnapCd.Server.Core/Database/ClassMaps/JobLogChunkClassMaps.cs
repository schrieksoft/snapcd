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

public class ModuleJobLogChunkClassMap : IEntityTypeConfiguration<ModuleJobLogChunk>
{
    public void Configure(EntityTypeBuilder<ModuleJobLogChunk> entity)
    {
        JobLogChunkMapping.Configure(entity, "ModuleJobLogChunks");

        entity
            .HasOne(e => e.Job)
            .WithMany()
            .HasForeignKey(e => new { e.JobId, e.OrganizationId })
            .HasPrincipalKey(j => new { j.Id, j.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class StateMigrationJobLogChunkClassMap : IEntityTypeConfiguration<StateMigrationJobLogChunk>
{
    public void Configure(EntityTypeBuilder<StateMigrationJobLogChunk> entity)
    {
        JobLogChunkMapping.Configure(entity, "StateMigrationJobLogChunks");

        entity
            .HasOne(e => e.Job)
            .WithMany()
            .HasForeignKey(e => new { e.JobId, e.OrganizationId })
            .HasPrincipalKey(j => new { j.Id, j.OrganizationId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal static class JobLogChunkMapping
{
    public static void Configure<T>(EntityTypeBuilder<T> entity, string table) where T : JobLogChunk
    {
        entity.ToTable(table, t => t.UseSqlOutputClause(false));

        entity.HasKey(e => new { e.Id, e.OrganizationId });

        entity.HasIndex(e => e.Id).IsUnique();

        entity
            .Property(e => e.Sequence)
            .UseIdentityColumn();

        entity
            .HasIndex(e => new { e.JobId, e.Sequence })
            .IsUnique()
            .IncludeProperties(e => new { e.ModuleId, e.TaskNames, e.LineCount, e.UncompressedBytes });

        entity
            .HasIndex(e => new { e.JobId, e.ModuleId, e.BatchTimeStamp })
            .IsUnique();
    }
}
