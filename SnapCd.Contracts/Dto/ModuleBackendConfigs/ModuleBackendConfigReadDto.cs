using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleBackendConfigs;

/// <summary>
/// DTO for ModuleBackendConfig responses (GET operations).
/// </summary>
[Obsolete("Use TerraformFlag entities instead.")]
public class ModuleBackendConfigReadDto : ModuleBackendConfigCreateDto, IDto
{
    public Guid Id { get; set; }
}
