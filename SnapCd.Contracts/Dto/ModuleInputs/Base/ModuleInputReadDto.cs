using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleInputs.Base;

public class ModuleInputReadDto : ModuleInputCreateDto, IDto
{
    public Guid Id { get; set; }
}