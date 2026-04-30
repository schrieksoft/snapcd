using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleInputs;

public class ModuleInputFromLiteralReadDto : ModuleInputFromLiteralCreateDto, IDto
{
    public Guid Id { get; set; }
}