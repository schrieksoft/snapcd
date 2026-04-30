using SnapCd.Contracts;
using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Events.System;

public class SourceRefreshCompleted : StepRequestBase
{
    public required string SourceUrl { get; set; }
    public required string SourceRevision { get; set; }
    public SourceType SourceType { get; set; } = SourceType.Git;
    public SourceRevisionType SourceRevisionType { get; set; } = SourceRevisionType.Default;

    public required string DefinitiveRevision { get; set; }
}