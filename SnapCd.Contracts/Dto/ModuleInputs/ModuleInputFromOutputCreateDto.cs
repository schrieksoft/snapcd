using SnapCd.Contracts.Dto.ModuleInputs.Base;

namespace SnapCd.Contracts.Dto.ModuleInputs;

public class ModuleInputFromOutputCreateDto : ModuleInputCreateDto
{
    public Guid OutputModuleId { get; set; }

    public string OutputName { get; set; } = null!;
}
