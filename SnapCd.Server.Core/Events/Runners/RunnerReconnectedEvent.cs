namespace SnapCd.Server.Core.Events.Runners;

public record RunnerReconnectedEvent
{
    public Guid OrganizationId { get; init; }
    public Guid RunnerId { get; init; }
    public string InstanceName { get; init; } = null!;
    public Guid ServerInstanceId { get; init; }
}
