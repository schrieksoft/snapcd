using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleHooks;

public class ModuleHookUpdateDto : ModuleHookCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
