using SnapCd.Server.Core.Services.ResolvedConfiguration.HelperClasses;

namespace SnapCd.Server.Core.Events.Jobs.Base;

public class ModuleJobEventBase : JobEventBase
{
    public Guid CorrelationId { get; set; }
    public ResolvedModule Declared { get; set; } = null!;
}