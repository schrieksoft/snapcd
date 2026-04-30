using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModulePulumiArrayFlags;

public class ModulePulumiArrayFlagReadDto : ModulePulumiArrayFlagCreateDto, IDto
{
    public Guid Id { get; set; }
}
