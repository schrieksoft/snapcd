namespace SnapCd.Server.Core.Events.System;

public class ApprovalReevaluationRequestedEvent
{
    public Guid ModuleId { get; set; }
    public Guid ModuleJobId { get; set; }
}
