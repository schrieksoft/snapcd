namespace SnapCd.Runner.Exceptions;

/// <summary>
/// Exception thrown when parsing Terraform files fails.
/// </summary>
public class VariableParseException : Exception
{
    /// <summary>
    /// The file path that failed to parse (if applicable).
    /// </summary>
    public string? FilePath { get; }

    public VariableParseException(string message, string? filePath = null)
        : base(message)
    {
        FilePath = filePath;
    }

    public VariableParseException(string message, Exception innerException, string? filePath = null)
        : base(message, innerException)
    {
        FilePath = filePath;
    }
}