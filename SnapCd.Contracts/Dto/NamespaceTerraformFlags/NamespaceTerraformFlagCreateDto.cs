using System.ComponentModel.DataAnnotations;

namespace SnapCd.Contracts.Dto.NamespaceTerraformFlags;

public class NamespaceTerraformFlagCreateDto
{
    public TerraformCommandTask Task { get; set; }

    public TerraformFlag Flag { get; set; }

    [MaxLength(1000)] public string? Value { get; set; }

    public Guid NamespaceId { get; set; }
}
