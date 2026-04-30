using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModulePulumiArrayFlags;

public class ModulePulumiArrayFlagUpdateDto : ModulePulumiArrayFlagCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
