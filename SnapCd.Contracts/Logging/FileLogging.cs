// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Serilog.Extensions.Logging;

namespace SnapCd.Contracts.Logging;

/// <summary>
/// Writes the terminal log to a file as well, when `Logging:File:Path` is set. Off unless
/// configured, so it costs nothing where nobody is reading the file.
///
/// The console log of a process run from an IDE exists only in that terminal, which is the one
/// place it cannot be read from afterwards.
/// </summary>
public static class FileLogging
{
    public static ILoggingBuilder AddSnapCdFileLogging(this ILoggingBuilder logging, IConfiguration configuration)
    {
        var path = configuration["Logging:File:Path"];

        if (string.IsNullOrWhiteSpace(path)) return logging;

        var minimum = Enum.TryParse<LogEventLevel>(configuration["Logging:File:MinimumLevel"], out var parsed)
            ? parsed
            : LogEventLevel.Debug;

        var serilog = new LoggerConfiguration()
            .MinimumLevel.Is(minimum)
            .WriteTo.File(
                path,
                rollingInterval: RollingInterval.Day,
                rollOnFileSizeLimit: true,
                fileSizeLimitBytes: 256 * 1024 * 1024,
                retainedFileCountLimit: 5,
                shared: true,
                outputTemplate: "{Timestamp:HH:mm:ss.fff} {Level:u4} {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        logging.AddProvider(new SerilogLoggerProvider(serilog, dispose: true));

        return logging;
    }
}
