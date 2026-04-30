using SnapCd.Contracts.Dto.ModuleInputs.Base;

namespace SnapCd.Contracts.Dto.ModuleInputs;

public class ModuleInputFromOutputSetCreateDto : ModuleInputCreateDto
{
    public Guid OutputModuleId { get; set; }
}
