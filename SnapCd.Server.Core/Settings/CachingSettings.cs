using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Settings;

public class CachingSettings
{
    public CacheProvider Provider { get; set; } = CacheProvider.InMemory;
    public string? ConnectionString { get; set; }
}
