namespace SnapCd.Server.Core.Events.System;

public class ApprovalTimeoutReceived
{
    public Guid OrganizationId { get; set; }
    public Guid CorrelationId { get; set; }
}