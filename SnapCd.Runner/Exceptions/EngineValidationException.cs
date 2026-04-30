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