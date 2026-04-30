using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModulePulumiFlags;

public class ModulePulumiFlagUpdateDto : ModulePulumiFlagCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
