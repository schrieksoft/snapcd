namespace SnapCd.Server.Core.Events.System;

public class NamespaceModifiedEvent
{
    public required Guid Id { get; set; }

    public required Guid OrganizationId { get; set; }
}