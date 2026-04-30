namespace SnapCd.Server.Core.Events.System;

public class SecretModifiedEvent
{
    public required Guid SecretId { get; set; }

    public required Guid OrganizationId { get; set; }
}