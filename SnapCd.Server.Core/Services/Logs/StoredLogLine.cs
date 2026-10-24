// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.Json.Serialization;
using Serilog.Events;
using SnapCd.Contracts;

namespace SnapCd.Server.Core.Services.Logs;

/// <summary>
/// What a stored log line keeps: only what varies per line. The job, Module, Namespace, Stack and batch are
/// known from the chunk and its job.
/// </summary>
public class StoredLogLine
{
    [JsonPropertyName("t")] public DateTimeOffset Timestamp { get; set; }

    [JsonPropertyName("k")] public string? TaskName { get; set; }

    [JsonPropertyName("l")] public LogEventLevel Level { get; set; }

    [JsonPropertyName("m")] public string? Message { get; set; }

    [JsonPropertyName("g")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Dictionary<string, string>? Tags { get; set; }

    [JsonPropertyName("s")] public LogSource Source { get; set; }
}
