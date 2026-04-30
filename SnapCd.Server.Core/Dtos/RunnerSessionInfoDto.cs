namespace SnapCd.Server.Core.Dtos;

public class RunnerSessionInfoDto
{
    public string ConnectionId { get; set; } = null!;
    public Guid RunnerId { get; set; }
    public Guid OrganizationId { get; set; }
    public string RunnerName { get; set; } = null!;
    public DateTime ConnectedAt { get; set; }
    public DateTime LastHeartbeat { get; set; }
}