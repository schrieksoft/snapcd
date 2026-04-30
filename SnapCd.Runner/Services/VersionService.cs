using System.Reflection;

namespace SnapCd.Runner.Services;

public interface IVersionService
{
    string Version { get; }
    string ShortVersion { get; }
}

public class VersionService : IVersionService
{
    private readonly string _version;

    public VersionService()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
        var versionAttribute = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();

        _version = versionAttribute?.InformationalVersion ?? "0.0.0";
    }

    public string Version => _version;

    // Returns version without build metadata (e.g., "1.2.3" instead of "1.2.3+sha.abc123")
    public string ShortVersion
    {
        get
        {
            var plusIndex = _version.IndexOf('+');
            return plusIndex > 0 ? _version.Substring(0, plusIndex) : _version;
        }
    }
}