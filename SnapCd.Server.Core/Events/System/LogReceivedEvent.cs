namespace SnapCd.Server.Core.Events.System;

public class LogReceivedEvent
{
    public Guid JobId { get; set; }

    public Guid ModuleId { get; set; }
}