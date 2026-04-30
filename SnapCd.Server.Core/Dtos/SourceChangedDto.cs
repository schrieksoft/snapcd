using SnapCd.Contracts;

namespace SnapCd.Server.Core.Dtos;

public class SourceChangedDto
{
    public string SourceUrl { get; set; } = null!;
    public string SourceRevision { get; set; } = null!;

    public SourceType SourceType { get; set; } = SourceType.Git;
}