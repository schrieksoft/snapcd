using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Dtos.ModuleJobs;

public class ModuleJobCreateDto
{
    public Guid ModuleId { get; set; }
    public DateTimeOffset TimestampStart { get; set; }
    public DateTimeOffset? TimestampEnd { get; set; }
    public ExecutionStatus Status { get; set; }
    public string JobType { get; set; } = null!;
    public bool? WaitingForApproval { get; set; }
    public bool? IsCurrent { get; set; }
    public string? DefinitiveRevision { get; set; }
    public ActualStateHeadline? ActualStateHeadline { get; set; }
}
