namespace SnapCd.Server.Core.Events.System;

public class StackModifiedEvent
{
    public required Guid Id { get; set; }

    public required Guid OrganizationId { get; set; }
}