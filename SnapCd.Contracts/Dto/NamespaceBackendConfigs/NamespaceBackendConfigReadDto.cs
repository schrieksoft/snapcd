using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.NamespaceBackendConfigs;

[Obsolete("Use TerraformFlag entities instead.")]
public class NamespaceBackendConfigReadDto : NamespaceBackendConfigCreateDto, IDto
{
    public Guid Id { get; set; }
}