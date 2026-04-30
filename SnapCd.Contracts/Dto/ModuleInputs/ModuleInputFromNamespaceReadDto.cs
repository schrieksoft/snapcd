using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleInputs;

public class ModuleInputFromNamespaceReadDto : ModuleInputFromNamespaceCreateDto, IDto
{
    public Guid Id { get; set; }
}