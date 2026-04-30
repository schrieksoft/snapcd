namespace SnapCd.Server.Core.Events.System;

public class ModuleDependencyCheckRequested
{
    public Guid ModuleId { get; set; }

    public Guid OrganizationId { get; set; }
}