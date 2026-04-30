using SnapCd.Contracts.Interfaces;

namespace SnapCd.Server.Core.Dtos;

/// <summary>
/// DTO for RunnerConnectionJob responses (GET operations).
/// Represents the association between a runner connection and a module job.
/// </summary>
public class RunnerConnectionJobReadDto : IDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid RunnerConnectionId { get; set; }
    public Guid ModuleJobId { get; set; }
    public string TaskName { get; set; } = null!;
    public DateTime CreatedDateTime { get; set; }
    public DateTime ModifiedDateTime { get; set; }
}