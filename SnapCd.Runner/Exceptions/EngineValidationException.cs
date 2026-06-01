// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

namespace SnapCd.Runner.Exceptions;

/// <summary>
/// Exception thrown when Terraform validation fails (terraform validate command returns non-zero exit code).
/// </summary>
public class EngineValidationException : Exception
{
    /// <summary>
    /// The working directory where terraform validate was executed.
    /// </summary>
    public string WorkingDirectory { get; }

    /// <summary>
    /// The exit code returned by the terraform validate process.
    /// </summary>
    public int ExitCode { get; }

    /// <summary>
    /// The error output from terraform validate (stderr).
    /// </summary>
    public string ErrorOutput { get; }

    public EngineValidationException(
        string message,
        string workingDirectory,
        int exitCode,
        string errorOutput)
        : base(message)
    {
        WorkingDirectory = workingDirectory;
        ExitCode = exitCode;
        ErrorOutput = errorOutput;
    }

    public EngineValidationException(
        string message,
        Exception innerException,
        string workingDirectory,
        int exitCode,
        string errorOutput)
        : base(message, innerException)
    {
        WorkingDirectory = workingDirectory;
        ExitCode = exitCode;
        ErrorOutput = errorOutput;
    }
}