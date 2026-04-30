using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Views;

public class DiscoveredOutput
{
    public Guid InputId { get; set; }
    public Guid ModuleId { get; set; }
    public Output? Output { get; set; }
}