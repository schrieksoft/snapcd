using SnapCd.Contracts;

namespace SnapCd.Server.Core.Events.Gatekeeping;

public class GatekeepingJobRequestedBase
{
    public required Guid ModuleId { get; set; }

    public required Guid OrganizationId { get; set; }

    public Guid? JobId { get; set; }

    //public Guid CorrelationId { get; set; }
    public DesiredStateHeadline DesiredStateHeadline { get; set; }
    public bool SetNewDesiredState { get; set; }

    public string? DefinitiveRevision { get; set; }

    public string? RunnerInstanceNameOverride { get; set; }
}