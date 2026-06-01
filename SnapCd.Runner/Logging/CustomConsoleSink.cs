// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using System.Text.RegularExpressions;
using Serilog.Core;
using Serilog.Events;
using SnapCd.Contracts.Dto;
using SnapCd.Contracts.Dto.Misc;
using SnapCd.Runner.Services;

namespace SnapCd.Runner.Logging;

public class CustomConsoleSink : ILogEventSink
{
    private readonly IFormatProvider? _formatProvider;

    public CustomConsoleSink(IFormatProvider? formatProvider = null)
    {
        _formatProvider = formatProvider;
    }

    public void Emit(LogEvent logEvent)
    {
        var stackName = LogMessageHelper.GetStringProperty(logEvent, nameof(LogEntryDto.StackName));
        var namespaceName = LogMessageHelper.GetStringProperty(logEvent, nameof(LogEntryDto.NamespaceName));
        var moduleName = LogMessageHelper.GetStringProperty(logEvent, nameof(LogEntryDto.ModuleName));
        var logContext = LogMessageHelper.GetStringProperty(logEvent, nameof(LogEntryDto.TaskName));

        var message = logEvent.Properties.TryGetValue(nameof(LogEntryDto.Message), out var messageValue)
            ? LogMessageHelper.TrimQuotes(Regex.Unescape(messageValue.ToString()))
            : string.Empty;

        // Format the output message
        var level = logEvent.Level.ToString().ToUpper().Substring(0, 3);
        var timestamp = logEvent.Timestamp.ToString("HH:mm:ss", _formatProvider);
        var exception = logEvent.Exception != null ? $"\n{logEvent.Exception}" : string.Empty;

        // Format: [Timestamp] [Level] Message

        var output =
            $"[{timestamp}] [{level}] | [{stackName}.{namespaceName}.{moduleName}] [{logContext}] {message}{exception}";
        if (namespaceName == "" && moduleName == "")
        {
            message = logEvent.RenderMessage(_formatProvider);
            output = $"[{timestamp}] [{level}] | {message}{exception}";
        }

        // Write to the console
        Console.WriteLine(output);
    }
}