// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Serilog.Events;

namespace SnapCd.Server.Core.Settings;

public class LoggingSettings
{
    public LogEventLevel SystemDefaultLogLevel { get; set; } = LogEventLevel.Error;
    public LogEventLevel SnapCdDefaultLogLevel { get; set; } = LogEventLevel.Information;

    public Dictionary<string, LogEventLevel> LogLevelOverrides { get; set; } = new();

    public int BatchSizeLimit { get; set; } = 50;
    public int PeriodSeconds { get; set; } = 5;
    public bool EarlyEmitFirstEvent { get; set; } = true;
}