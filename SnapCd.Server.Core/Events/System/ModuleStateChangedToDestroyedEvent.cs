namespace SnapCd.Server.Core.Events.System;

public class ModuleStateChangedToDestroyedEvent
{
    public required Guid ModuleId { get; set; }

    public required Guid OrganizationId { get; set; }
}