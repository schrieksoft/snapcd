namespace SnapCd.Runner.Exceptions;

/// <summary>
/// Exception thrown when a hook fails pre-approval validation.
/// </summary>
public class HookNotPreapprovedException : Exception
{
    public string HookName { get; }
    public string HookContentPreview { get; }

    public HookNotPreapprovedException(string hookName, string hookContent)
        : base($"Hook '{hookName}' is not pre-approved and cannot be executed. " +
               $"Enable hook pre-approval validation and add this hook to the pre-approved hooks directory.")
    {
        HookName = hookName;
        // Only store first 100 characters for security/logging purposes
        HookContentPreview = hookContent.Length > 100
            ? hookContent.Substring(0, 100) + "..."
            : hookContent;
    }
}
