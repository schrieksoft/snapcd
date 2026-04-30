namespace SnapCd.Server.Core.Events.System;

public class ResourceCountRefreshedEvent
{
    public Guid JobId { get; set; }

    public Guid ModuleId { get; set; }

    public Guid OrganizationId { get; set; }

    public int? ActualResourceCount { get; set; }
}