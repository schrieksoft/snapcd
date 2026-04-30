using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleInputs;

public class ModuleInputFromOutputSetUpdateDto : ModuleInputFromOutputSetCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
