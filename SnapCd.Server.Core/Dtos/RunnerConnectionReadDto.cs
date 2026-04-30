using SnapCd.Contracts.Interfaces;

namespace SnapCd.Server.Core.Dtos;

/// <summary>
/// DTO for RunnerConnection responses (GET operations).
/// Represents an active runner connection to a specific server instance.
/// </summary>
public class RunnerConnectionReadDto : IDto
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid RunnerId { get; set; }
    public string InstanceName { get; set; } = null!;
    public string ConnectionId { get; set; } = null!;
    public Guid ServerInstanceId { get; set; }
    public DateTime CreatedDateTime { get; set; }
    public DateTime ModifiedDateTime { get; set; }
}