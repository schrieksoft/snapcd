using SnapCd.Contracts.Dto;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Contracts.Dto.Misc;

namespace SnapCd.Contracts.RunnerRequests;


public class GetModuleRequestBase : TaskRequestBase
{
    public SourceType SourceType { get; set; }
    public SourceRevisionType SourceRevisionType { get; set; }
    public required string SourceUrl { get; set; }
    public required string SourceRevision { get; set; }
    public string? SourceDefinitiveRevision { get; set; }
    public string? SourceSemanticVersion { get; set; }
    public string Engine { get; set; } = "tofu";
    public bool CleanInitEnabled { get; set; }
    public List<ExtraFileDto>? ExtraFiles { get; set; }
}
