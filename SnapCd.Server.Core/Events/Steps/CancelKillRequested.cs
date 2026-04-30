using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Events.Steps;

public class CancelKillRequested : CorrelationBase
{
    public string? RunnerInstanceName { get; set; }
    public Guid RunnerId { get; set; }
}