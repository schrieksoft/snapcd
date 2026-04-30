using SnapCd.Server.Core.Settings.Interfaces;

namespace SnapCd.Server.Core.Settings.Repositories;

/// <summary>
/// Repository settings for RunnerConnection entity.
/// Events are disabled since connections are runtime state, not configuration changes.
/// </summary>
public class RunnerConnectionRepositorySettings : IEntitySettings
{
    public bool EmitCreateEvents { get; set; } = false;
    public bool EmitUpdateEvents { get; set; } = false;
    public bool EmitDeleteEvents { get; set; } = false;
    public TimeSpan EventTtl { get; set; } = TimeSpan.FromMinutes(30);
}
