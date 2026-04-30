using SnapCd.Contracts.Dto.VariableSets;

namespace SnapCd.Runner.Services;

public class PulumiVariableDiscoveryService : IVariableDiscoveryService
{
    public Task<VariableSetCreateDto?> CreateVariableSet(
        string directoryPath,
        Guid moduleId,
        ISet<string>? extraFileNames = null)
    {
        // Pulumi variables are defined in application code (TypeScript/Python/Go/C#),
        // not in declarative files we can reliably parse. Returning null means no
        // VariableSet is stored, so OutputSetParamResolver skips filtering and all
        // selected outputs are injected as inputs.
        return Task.FromResult<VariableSetCreateDto?>(null);
    }

    public Task<Dictionary<string, bool>> DiscoverOutputSourcesAsync(
        string directoryPath,
        ISet<string>? extraFileNames = null)
    {
        // Pulumi outputs are defined in code, not in declarative files we can scan.
        // Return empty - outputs will be discovered at runtime via `pulumi stack output`.
        return Task.FromResult(new Dictionary<string, bool>());
    }
}
