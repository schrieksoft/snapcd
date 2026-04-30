using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleInputs;

public class ModuleInputFromOutputSetReadDto : ModuleInputFromOutputSetCreateDto, IDto
{
    public Guid Id { get; set; }
}