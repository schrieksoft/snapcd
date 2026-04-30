using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleInputs;

public class ModuleInputFromDefinitionUpdateDto : ModuleInputFromDefinitionCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
