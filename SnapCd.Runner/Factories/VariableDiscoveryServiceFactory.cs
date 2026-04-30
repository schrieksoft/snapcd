using SnapCd.Runner.Services;

namespace SnapCd.Runner.Factories;

public class VariableDiscoveryServiceFactory
{
    public IVariableDiscoveryService Create(string engine)
    {
        return engine switch
        {
            "terraform" or "tofu" => new TerraformVariableDiscoveryService(),
            "pulumi" => new PulumiVariableDiscoveryService(),
            _ => throw new NotSupportedException($"Engine '{engine}' is not supported for variable discovery")
        };
    }
}
