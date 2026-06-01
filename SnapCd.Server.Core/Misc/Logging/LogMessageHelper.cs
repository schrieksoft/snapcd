// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Serilog.Events;

namespace SnapCd.Server.Core.Misc.Logging;

public class LogMessageHelper
{
    public static string GetStringProperty(LogEvent logEvent, string propertyName)
    {
        return logEvent.Properties.TryGetValue(propertyName, out var value)
            ? value.ToString().Trim('"')
            : string.Empty;
    }

    public static Guid GetGuidProperty(LogEvent logEvent, string propertyName)
    {
        return logEvent.Properties.TryGetValue(propertyName, out var value) &&
               Guid.TryParse(value.ToString().Trim('"'), out var guid)
            ? guid
            : Guid.Empty;
    }

    public static string TrimQuotes(string value)
    {
        if (value.StartsWith('"') && value.EndsWith('"'))
            return value.Substring(1, value.Length - 2);
        return value;
    }
}