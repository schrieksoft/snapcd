using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleInputs;

public class ModuleInputFromOutputDtoRead : ModuleInputFromOutputCreateDto, IDto
{
    public Guid Id { get; set; }
}