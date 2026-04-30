namespace SnapCd.Server.Core.Events.Server;

/// <summary>
/// Response message indicating whether a server instance still has the specified runner connection active.
/// </summary>
public class ServerHeartbeatResponse
{
    public bool IsConnected { get; set; }
}
