using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleInputs.Base;

public class ModuleInputUpdateDto : ModuleInputCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
