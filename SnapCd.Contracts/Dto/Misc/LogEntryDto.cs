// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Serilog.Events;

namespace SnapCd.Contracts.Dto.Misc;

public class LogEntryDto
{
    public Guid JobId { get; set; }
    public DateTimeOffset Timestamp { get; set; }

    public DateTimeOffset BatchTimeStamp { get; set; }

    public Guid? StackId { get; set; }


    public Guid? NamespaceId { get; set; }

    public Guid ModuleId { get; set; }

    public string StackName { get; set; } = null!;

    public string NamespaceName { get; set; } = null!;
    public string? ModuleName { get; set; }

    public LogEventLevel Level { get; set; }
    public string Message { get; set; } = null!;

    public string TaskName { get; set; } = null!;

    public Dictionary<string, string>? Tags { get; set; }

    public LogSource Source { get; set; } = LogSource.Runner;
}