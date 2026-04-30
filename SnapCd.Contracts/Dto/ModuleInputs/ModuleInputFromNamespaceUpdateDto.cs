using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleInputs;

public class ModuleInputFromNamespaceUpdateDto : ModuleInputFromNamespaceCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
