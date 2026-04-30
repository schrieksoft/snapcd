using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.NamespaceBackendConfigs;

[Obsolete("Use TerraformFlag entities instead.")]
public class NamespaceBackendConfigUpdateDto : NamespaceBackendConfigCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
