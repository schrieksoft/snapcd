namespace SnapCd.Contracts.RunnerRequests.HelperClasses;

public class EngineFlagEntry
{
    public string Flag { get; set; } = null!;
    public string? Value { get; set; }
}

public class EngineArrayFlagEntry
{
    public string Flag { get; set; } = null!;
    public string Value { get; set; } = null!;
}
