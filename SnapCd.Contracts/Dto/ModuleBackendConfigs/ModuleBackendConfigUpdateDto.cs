using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleBackendConfigs;

/// <summary>
/// DTO for updating an existing ModuleBackendConfig (PUT operations).
/// </summary>
[Obsolete("Use TerraformFlag entities instead.")]
public class ModuleBackendConfigUpdateDto : ModuleBackendConfigCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
