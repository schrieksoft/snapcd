// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using Serilog.Events;

namespace SnapCd.Contracts.Dto.Misc;

/// <summary>
/// What JobLogService needs of a log line to append, order and group it. Each table has its own
/// log type, and this is only what the shared logic reads.
/// </summary>
public interface IJobLogEntry
{
    Guid JobId { get; set; }
    DateTimeOffset Timestamp { get; set; }
    DateTimeOffset BatchTimeStamp { get; set; }
    Guid ModuleId { get; set; }
    string Message { get; set; }
    string TaskName { get; set; }

    // Written as Serilog context by the stream and read back by the sink, so the names have to
    // agree. Declared here rather than nameof'd off a type parameter, which C# does not allow.
    Guid? StackId { get; set; }
    Guid? NamespaceId { get; set; }
    string StackName { get; set; }
    string NamespaceName { get; set; }
    string? ModuleName { get; set; }
    LogEventLevel Level { get; set; }
    LogSource Source { get; set; }
}
