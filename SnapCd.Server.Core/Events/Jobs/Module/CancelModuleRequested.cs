using SnapCd.Contracts;
using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Events.Jobs.Module;

public class CancelModuleRequested : CorrelationBase
{
    public CancellationType CancellationType { get; set; }
}