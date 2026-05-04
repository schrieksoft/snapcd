namespace SnapCd.Contracts.RunnerRequests.HelperClasses;

/// <summary>
/// Engine flag configuration sent with the Init request.
/// </summary>
public class EngineBackendConfiguration
{
    public List<PulumiFlagEntry> PulumiFlags { get; set; } = [];
    public List<PulumiArrayFlagEntry> PulumiArrayFlags { get; set; } = [];

    public List<TerraformFlagEntry> TerraformFlags { get; set; } = [];
    public List<TerraformArrayFlagEntry> TerraformArrayFlags { get; set; } = [];
}

public class PulumiFlagEntry
{
    public PulumiCommandTask Task { get; set; }
    public PulumiFlag Flag { get; set; }
    public string? Value { get; set; }
}

public class PulumiArrayFlagEntry
{
    public PulumiCommandTask Task { get; set; }
    public PulumiArrayFlag Flag { get; set; }
    public string Value { get; set; } = null!;
}

public class TerraformFlagEntry
{
    public TerraformCommandTask Task { get; set; }
    public TerraformFlag Flag { get; set; }
    public string? Value { get; set; }
}

public class TerraformArrayFlagEntry
{
    public TerraformCommandTask Task { get; set; }
    public TerraformArrayFlag Flag { get; set; }
    public string Value { get; set; } = null!;
}
