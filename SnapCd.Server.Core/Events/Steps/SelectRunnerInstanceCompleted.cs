using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Events.Steps;

public class SelectRunnerInstanceCompleted : StepResponseBase
{
    public string RunnerInstanceName { get; set; } = null!;
}