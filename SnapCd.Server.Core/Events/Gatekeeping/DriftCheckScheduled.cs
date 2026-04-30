namespace SnapCd.Server.Core.Events.Gatekeeping;

public class DriftCheckScheduled
{
    public Guid ModuleId { get; set; }
    public Guid OrganizationId { get; set; }
}
