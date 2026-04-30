using System.ComponentModel.DataAnnotations;

namespace SnapCd.Contracts.Dto.ModulePulumiFlags;

public class ModulePulumiFlagCreateDto
{
    public PulumiCommandTask Task { get; set; }

    public PulumiFlag Flag { get; set; }

    [MaxLength(1000)] public string? Value { get; set; }

    public Guid ModuleId { get; set; }
}
