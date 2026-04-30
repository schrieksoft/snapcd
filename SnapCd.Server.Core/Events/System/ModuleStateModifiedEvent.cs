namespace SnapCd.Server.Core.Events.System;

public class ModuleStateModifiedEvent
{
    public Guid ModuleId { get; set; }

    public Guid OrganizationId { get; set; }
}