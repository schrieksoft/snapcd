using System.ComponentModel.DataAnnotations;

namespace SnapCd.Contracts.Dto.ModulePulumiArrayFlags;

public class ModulePulumiArrayFlagCreateDto
{
    public PulumiCommandTask Task { get; set; }

    public PulumiArrayFlag Flag { get; set; }

    [Required] [MaxLength(1000)] public string Value { get; set; } = null!;

    public Guid ModuleId { get; set; }
}
