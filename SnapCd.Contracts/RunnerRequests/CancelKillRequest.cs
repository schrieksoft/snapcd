namespace SnapCd.Contracts.RunnerRequests;

/// <summary>
/// Request sent from server to runner via SignalR to perform a kill cancellation.
/// Runner should respond by calling CancelKillResponse on the RunnerHub.
/// </summary>
public class CancelKillRequest
{ 
    public Guid JobId { get; set; }
}
