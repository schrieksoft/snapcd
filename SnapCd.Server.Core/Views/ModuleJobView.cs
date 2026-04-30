using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;

namespace SnapCd.Server.Core.Views;

/// <summary>
/// View model for ModuleJob used for component state persistence.
/// Contains only the properties needed for UI rendering without navigation properties.
/// </summary>
public class ModuleJobView
{
    public required Guid Id { get; init; }
    public required DateTimeOffset TimestampStart { get; init; }
    public required ExecutionStatus Status { get; set; }
    public required string JobType { get; init; }
    public ServerSideStep? FailedOnServerSideStep { get; set; }
    public string? ServerSideErrorHeader { get; set; }
    public string? ServerSideError { get; set; }
    public bool? WaitingForApproval { get; set; }
}