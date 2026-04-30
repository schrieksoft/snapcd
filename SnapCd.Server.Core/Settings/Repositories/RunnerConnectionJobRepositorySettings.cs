using SnapCd.Server.Core.Settings.Interfaces;

namespace SnapCd.Server.Core.Settings.Repositories;

/// <summary>
/// Repository settings for RunnerConnectionJob entity.
/// Events are disabled since this tracks runtime job-to-connection associations, not configuration changes.
/// </summary>
public class RunnerConnectionJobRepositorySettings : IEntitySettings
{
    public bool EmitCreateEvents { get; set; } = false;
    public bool EmitUpdateEvents { get; set; } = false;
    public bool EmitDeleteEvents { get; set; } = false;
    public TimeSpan EventTtl { get; set; } = TimeSpan.FromMinutes(30);
}
