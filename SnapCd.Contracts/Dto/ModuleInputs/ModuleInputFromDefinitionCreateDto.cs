using SnapCd.Contracts.Dto.ModuleInputs.Base;

namespace SnapCd.Contracts.Dto.ModuleInputs;

public class ModuleInputFromDefinitionCreateDto : ModuleInputCreateDto
{
    public DefinitionInputType DefinitionName { get; set; }
}
