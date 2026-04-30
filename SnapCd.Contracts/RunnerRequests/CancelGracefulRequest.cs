namespace SnapCd.Contracts.RunnerRequests;

/// <summary>
/// Request sent from server to runner via SignalR to perform a graceful cancellation.
/// Runner should respond by calling CancelGracefulResponse on the RunnerHub.
/// </summary>
public class CancelGracefulRequest
{ 
    public Guid JobId { get; set; }
}
