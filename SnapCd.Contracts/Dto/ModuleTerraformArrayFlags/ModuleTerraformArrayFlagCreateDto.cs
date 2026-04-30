using System.ComponentModel.DataAnnotations;

namespace SnapCd.Contracts.Dto.ModuleTerraformArrayFlags;

public class ModuleTerraformArrayFlagCreateDto
{
    public TerraformCommandTask Task { get; set; }

    public TerraformArrayFlag Flag { get; set; }

    [Required] [MaxLength(1000)] public string Value { get; set; } = null!;

    public Guid ModuleId { get; set; }
}
