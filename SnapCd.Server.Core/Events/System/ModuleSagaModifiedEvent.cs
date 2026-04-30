namespace SnapCd.Server.Core.Events.System;

public class ModuleSagaModifiedEvent
{
    public Guid ModuleId { get; set; }

    public Guid OrganizationId { get; set; }
}