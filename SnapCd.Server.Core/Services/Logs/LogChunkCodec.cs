// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.IO.Compression;
using System.Text.Json;
using System.Text.Json.Serialization;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Services.Logs;

public static class LogChunkCodec
{
    // Enum names, not numbers: the payload outlives any reordering of the enums.
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new JsonStringEnumConverter() }
    };

    /// <summary>Fills a chunk's payload and summary columns from one Module's lines of one batch.</summary>
    public static void Encode(JobLogChunk chunk, IReadOnlyList<IJobLogEntry> entries)
    {
        var lines = entries.Select(e => new StoredLogLine
        {
            Timestamp = e.Timestamp,
            TaskName = e.TaskName,
            Level = e.Level,
            Message = e.Message,
            Tags = e.Tags is { Count: > 0 } ? e.Tags : null,
            Source = e.Source,
            LogCorrelationId = e.LogCorrelationId,
            LogCorrelationDiscriminator = e.LogCorrelationDiscriminator
        }).ToList();

        var json = JsonSerializer.SerializeToUtf8Bytes(lines, Options);

        using var buffer = new MemoryStream();
        using (var gzip = new GZipStream(buffer, CompressionLevel.Optimal, leaveOpen: true))
            gzip.Write(json);

        chunk.Payload = buffer.ToArray();
        chunk.BatchTimeStamp = entries[0].BatchTimeStamp;
        chunk.UncompressedBytes = json.Length;
        chunk.LineCount = lines.Count;
        chunk.FirstTimestamp = lines.Min(l => l.Timestamp);
        chunk.LastTimestamp = lines.Max(l => l.Timestamp);
        chunk.TaskNames = string.Join('\n', lines.Select(l => l.TaskName ?? JobLogs.DefaultTaskName).Distinct());
    }

    public static List<StoredLogLine> Decode(byte[] payload)
    {
        using var gzip = new GZipStream(new MemoryStream(payload), CompressionMode.Decompress);
        return JsonSerializer.Deserialize<List<StoredLogLine>>(gzip, Options) ?? [];
    }
}
