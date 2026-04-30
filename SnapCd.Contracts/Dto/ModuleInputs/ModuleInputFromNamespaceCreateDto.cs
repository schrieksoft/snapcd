using SnapCd.Contracts.Dto.ModuleInputs.Base;

namespace SnapCd.Contracts.Dto.ModuleInputs;

public class ModuleInputFromNamespaceCreateDto : ModuleInputCreateDto
{
    public Guid NamespaceInputId { get; set; }
}
