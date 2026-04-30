using SnapCd.Server.Core.Settings.Interfaces;

namespace SnapCd.Server.Core.Settings.Repositories;

public class OutputSetRepositorySettings : IEntitySettings
{
    public bool EmitCreateEvents { get; set; } = true;
    public bool EmitUpdateEvents { get; set; } = true;
    public bool EmitDeleteEvents { get; set; } = true;
    public TimeSpan EventTtl { get; set; } = TimeSpan.FromMinutes(30);
}