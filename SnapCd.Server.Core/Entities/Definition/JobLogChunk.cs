// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace SnapCd.Server.Core.Entities.Definition;

/// <summary>
/// One Module's lines from one batch the runner sent, compressed. Written once and never updated, so
/// adding to a log costs the same however long the log already is.
/// </summary>
public abstract class JobLogChunk
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid JobId { get; set; }

    /// <summary>Insert order across all chunks; a job's log reads in this order.</summary>
    public long Sequence { get; set; }

    /// <summary>The Module the lines came from. A transfer runs two Modules under one job.</summary>
    public Guid ModuleId { get; set; }

    /// <summary>When the runner built the batch. One batch is one chunk, so a resent batch is recognised by it.</summary>
    public DateTimeOffset BatchTimeStamp { get; set; }

    public DateTimeOffset FirstTimestamp { get; set; }

    public DateTimeOffset LastTimestamp { get; set; }

    /// <summary>The tasks with lines in this chunk, newline separated.</summary>
    [MaxLength(1000)] public string TaskNames { get; set; } = null!;

    public int LineCount { get; set; }

    public int UncompressedBytes { get; set; }

    /// <summary>Gzipped JSON array of <see cref="SnapCd.Server.Core.Services.Logs.StoredLogLine"/>.</summary>
    public byte[] Payload { get; set; } = null!;
}

public class ModuleJobLogChunk : JobLogChunk
{
    [JsonIgnore] public ModuleJob Job { get; set; } = null!;
}

public class StateMigrationJobLogChunk : JobLogChunk
{
    [JsonIgnore] public StateMigrationJob Job { get; set; } = null!;
}
