using System.ComponentModel.DataAnnotations;

namespace SnapCd.Contracts.Dto.NamespaceTerraformArrayFlags;

public class NamespaceTerraformArrayFlagCreateDto
{
    public TerraformCommandTask Task { get; set; }

    public TerraformArrayFlag Flag { get; set; }

    [Required] [MaxLength(1000)] public string Value { get; set; } = null!;

    public Guid NamespaceId { get; set; }
}
