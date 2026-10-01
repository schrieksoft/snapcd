// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Misc.Helpers;

/// <summary>
/// Fitting an error to the column that holds it. A header is a fixed 255 and a body is 16000, and
/// what arrives is a runner's exception message, which carries a path and the engine's own output
/// and is bounded by nothing. Writing one over its column throws, and the throw lands in a saga
/// consume that is recording a failure, so the job stops where it is rather than failing cleanly.
/// </summary>
public static class ErrorText
{
    public const int HeaderLength = 255;
    public const int BodyLength = 16000;

    /// <summary>A last guard at the write, for a header composed somewhere this does not see.</summary>
    public static string? FitHeader(string? value) => Fit(value, HeaderLength);

    public static string? Body(string? value) => Fit(value, BodyLength);

    /// <summary>
    /// What failed, for the line the page shows beside a step. A label the server composes, not a
    /// slice of the engine's output: that goes in the body, where there is room for it.
    /// </summary>
    public static string? Header(string task, StateMigrationStepStatus status) =>
        Fit($"{task} {status.ToString().ToLowerInvariant()}", HeaderLength);

    /// <summary>The message and its stack trace, which is what someone opens the step to read.</summary>
    public static string? Detail(string? message, string? stackTrace) =>
        Body(string.IsNullOrEmpty(stackTrace)
            ? message
            : $"{message}\n\nStack Trace:\n{stackTrace}");

    private static string? Fit(string? value, int maxLength) =>
        string.IsNullOrEmpty(value) || value.Length <= maxLength
            ? value
            : value[..maxLength];
}
