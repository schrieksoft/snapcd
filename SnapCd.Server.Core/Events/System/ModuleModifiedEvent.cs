namespace SnapCd.Server.Core.Events.System;

public class ModuleModifiedEvent
{
    public required Guid Id { get; set; }

    public required Guid OrganizationId { get; set; }
}