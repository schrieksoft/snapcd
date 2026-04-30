using System.ComponentModel.DataAnnotations;

namespace SnapCd.Contracts.Dto.NamespaceBackendConfigs;

[Obsolete("Use TerraformFlag entities instead.")]
public class NamespaceBackendConfigCreateDto
{
    [Required] [MaxLength(255)] public string Name { get; set; } = string.Empty;

    [MaxLength(1000)] public string Value { get; set; } = null!;

    public Guid NamespaceId { get; set; }
}
