using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Views;

public class DiscoveredOutputSet
{
    public Guid InputId { get; set; }
    public Guid ModuleId { get; set; }
    public OutputSet? OutputSet { get; set; }
}