namespace SnapCd.Contracts.RunnerRequests;

public abstract class EngineJobRequestBase : TaskRequestBase
{
    public string Engine { get; set; } = null!;
}
