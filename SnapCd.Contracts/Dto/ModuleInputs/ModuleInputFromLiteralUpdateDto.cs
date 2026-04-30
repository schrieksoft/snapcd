using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleInputs;

public class ModuleInputFromLiteralUpdateDto : ModuleInputFromLiteralCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
