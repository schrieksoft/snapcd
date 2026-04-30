namespace SnapCd.Server.Core.Events.Jobs.Base;

public class JobEventBase
{
    public Guid ModuleId { get; set; }

    public Guid OrganizationId { get; set; }
}