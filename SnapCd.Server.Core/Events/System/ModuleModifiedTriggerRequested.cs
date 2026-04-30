namespace SnapCd.Server.Core.Events.System;

public class ModuleModifiedTriggerRequested
{
    public required Guid ModuleId { get; set; }

    public required Guid OrganizationId { get; set; }
}

public class ModuleModifiedWaitForNextTimeoutScheduled
{
    public required Guid CorrelationId { get; set; }

    public required Guid OrganizationId { get; set; }
}