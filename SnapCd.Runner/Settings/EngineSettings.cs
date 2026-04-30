using SnapCd.Runner.Utils;

namespace SnapCd.Runner.Settings;

public class EngineSettings
{
    private List<string> _additionalBinaryPaths = new();

    public List<string> AdditionalBinaryPaths
    {
        get => _additionalBinaryPaths;
        set => _additionalBinaryPaths = value.Select(PathUtils.ExpandTilde).ToList();
    }
}
