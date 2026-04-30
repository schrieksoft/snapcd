using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleInputs;

public class ModuleInputFromOutputUpdateDto : ModuleInputFromOutputCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
