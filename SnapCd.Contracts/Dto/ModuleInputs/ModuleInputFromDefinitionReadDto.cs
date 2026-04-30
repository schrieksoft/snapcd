using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleInputs;

public class ModuleInputFromDefinitionReadDto : ModuleInputFromDefinitionCreateDto, IDto
{
    public Guid Id { get; set; }
}