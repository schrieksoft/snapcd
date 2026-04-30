using SnapCd.Contracts.Dto.VariableSets;

namespace SnapCd.Runner.Services;

public interface IVariableDiscoveryService
{
    Task<VariableSetCreateDto?> CreateVariableSet(
        string directoryPath,
        Guid moduleId,
        ISet<string>? extraFileNames = null);

    Task<Dictionary<string, bool>> DiscoverOutputSourcesAsync(
        string directoryPath,
        ISet<string>? extraFileNames = null);
}
