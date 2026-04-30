using SnapCd.Contracts.RunnerRequests.HelperClasses;

namespace SnapCd.Contracts.RunnerRequests;


public class GetDefinitiveRevisionRequest : TaskRequestBase
{
    public SourceType SourceType { get; set; }
    public SourceRevisionType SourceRevisionType { get; set; }
    public required string SourceUrl { get; set; }
    public required string SourceRevision { get; set; }
    public string? SourceDefinitiveRevision { get; set; }
    public string? SourceSemanticVersion { get; set; }
}
