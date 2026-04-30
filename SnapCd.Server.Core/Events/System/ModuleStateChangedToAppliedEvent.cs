namespace SnapCd.Server.Core.Events.System;

public class ModuleStateChangedToAppliedEvent
{
    public required Guid ModuleId { get; set; }

    public required Guid OrganizationId { get; set; }
}