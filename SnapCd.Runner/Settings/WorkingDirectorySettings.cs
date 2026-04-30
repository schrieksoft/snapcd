using SnapCd.Runner.Utils;

namespace SnapCd.Runner.Settings;

public class WorkingDirectorySettings
{
    private string _workingDirectory = string.Empty;
    private string _tempDirectory = string.Empty;

    public string WorkingDirectory
    {
        get => _workingDirectory;
        set => _workingDirectory = PathUtils.ExpandTilde(value);
    }

    public string TempDirectory
    {
        get => _tempDirectory;
        set => _tempDirectory = PathUtils.ExpandTilde(value);
    }
}