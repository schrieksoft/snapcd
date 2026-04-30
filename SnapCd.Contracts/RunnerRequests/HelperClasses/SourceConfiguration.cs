namespace SnapCd.Contracts.RunnerRequests.HelperClasses;

/// <summary>
/// Configuration for retrieving module source code.
/// Used by GetDefinitiveRevision and GetModule tasks.
/// </summary>
public class SourceConfiguration
{
    public SourceType SourceType { get; set; }
    public SourceRevisionType SourceRevisionType { get; set; }
    public required string SourceUrl { get; set; }
    public required string SourceRevision { get; set; }
}
