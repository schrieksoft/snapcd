using System.Reflection;

namespace SnapCd.Server.Core.Services.ViewManagement;

public class ViewAssemblySource(Assembly assembly, string resourcePrefix)
{
    public Assembly Assembly { get; } = assembly;
    public string ResourcePrefix { get; } = resourcePrefix;
}
