namespace SnapCd.Server.Core.Events.Jobs.Base;

public class ModuleJobEventCompletedBase : JobEventBase
{
    public Guid ModuleJobId { get; set; }
}