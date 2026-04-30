namespace SnapCd.Server.Core.Events.System;

public class ModuleResourceCountUpdatedEvent
{
    public Guid ModuleId { get; set; }

    public Guid OrganizationId { get; set; }
}