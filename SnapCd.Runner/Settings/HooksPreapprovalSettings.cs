using SnapCd.Runner.Utils;

namespace SnapCd.Runner.Settings;

public class HooksPreapprovalSettings
{
    private string _preapprovedHooksDirectory = string.Empty;

    /// <summary>
    /// Enable or disable hook pre-approval validation.
    /// When enabled, all incoming hooks must match a pre-approved hook from the PreapprovedHooksDirectory.
    /// </summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// Directory containing pre-approved hook scripts.
    /// Each file in this directory is considered a pre-approved hook.
    /// File names don't matter - only file content is used for validation.
    /// </summary>
    public string PreapprovedHooksDirectory
    {
        get => _preapprovedHooksDirectory;
        set => _preapprovedHooksDirectory = PathUtils.ExpandTilde(value);
    }
}
