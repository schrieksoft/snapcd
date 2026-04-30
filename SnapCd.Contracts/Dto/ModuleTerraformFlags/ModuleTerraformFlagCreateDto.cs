using System.ComponentModel.DataAnnotations;

namespace SnapCd.Contracts.Dto.ModuleTerraformFlags;

public class ModuleTerraformFlagCreateDto
{
    public TerraformCommandTask Task { get; set; }

    public TerraformFlag Flag { get; set; }

    [MaxLength(1000)] public string? Value { get; set; }

    public Guid ModuleId { get; set; }
}
