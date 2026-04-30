using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Events.Steps;

public class GetDefinitiveRevisionCompleted : StepResponseBase
{
    public required string DefinitiveRevision { get; set; } = string.Empty;
}