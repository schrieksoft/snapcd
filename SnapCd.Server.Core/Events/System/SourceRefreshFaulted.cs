using SnapCd.Contracts;

namespace SnapCd.Server.Core.Events.System;

public class SourceRefreshFaulted
{
    public required string SourceUrl { get; set; }
    public required string SourceRevision { get; set; }
    public SourceType SourceType { get; set; } = SourceType.Git;
    public SourceRevisionType SourceRevisionType { get; set; } = SourceRevisionType.Default;
    public string? ErrorMessage { get; set; }
    public string? StackTrace { get; set; }
}