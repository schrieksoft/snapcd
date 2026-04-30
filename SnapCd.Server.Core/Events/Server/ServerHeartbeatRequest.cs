namespace SnapCd.Server.Core.Events.Server;

/// <summary>
/// Request message to check if a specific server instance still has an active runner connection.
/// Sent to a server's fanout endpoint to verify connection validity during duplicate detection.
/// </summary>
public class ServerHeartbeatRequest
{
    public Guid ServerInstanceId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid RunnerId { get; set; }
    public string InstanceName { get; set; } = null!;
}
