using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleHooks;

public class ModuleHookReadDto : ModuleHookCreateDto, IDto
{
    public Guid Id { get; set; }
}
