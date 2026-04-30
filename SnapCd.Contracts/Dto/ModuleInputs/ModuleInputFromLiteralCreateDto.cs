using SnapCd.Contracts.Dto.ModuleInputs.Base;

namespace SnapCd.Contracts.Dto.ModuleInputs;

public class ModuleInputFromLiteralCreateDto : ModuleInputCreateDto
{
    public string LiteralValue { get; set; } = null!;

    public InputType Type { get; set; }
}
