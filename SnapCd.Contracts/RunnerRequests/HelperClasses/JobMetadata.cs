namespace SnapCd.Contracts.RunnerRequests.HelperClasses;

/// <summary>
/// Lightweight metadata about a job for logging and identification.
/// Replaces sending the full ResolvedModule object to runners.
/// </summary>
public class JobMetadata
{
    public required string ModuleName { get; set; }
    public required string NamespaceName { get; set; }
    public required string StackName { get; set; }
    public Guid ModuleId { get; set; }
    public required string SourceSubdirectory { get; set; }
}
