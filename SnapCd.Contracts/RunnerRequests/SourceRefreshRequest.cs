namespace SnapCd.Contracts.RunnerRequests;

/// <summary>
/// Request sent from server to runner via SignalR to refresh source information (resolve git references to commit SHAs).
/// This is a stateless operation - no JobId or correlation needed.
/// </summary>
public class SourceRefreshRequest
{
    public Guid OrganizationId { get; set; }
    public required string SourceUrl { get; set; }
    public required string SourceRevision { get; set; }
    public SourceType SourceType { get; set; } = SourceType.Git;
    public SourceRevisionType SourceRevisionType { get; set; } = SourceRevisionType.Default;
}
