namespace SnapCd.Contracts.Dto.ModuleInputs.Base;

public class ModuleInputCreateDto
{
    public Guid ModuleId { get; set; }
    public string Name { get; set; } = null!;
    public InputKind InputKind { get; set; }
}
