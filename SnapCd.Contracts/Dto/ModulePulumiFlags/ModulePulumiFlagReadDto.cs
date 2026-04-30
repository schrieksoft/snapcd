using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModulePulumiFlags;

public class ModulePulumiFlagReadDto : ModulePulumiFlagCreateDto, IDto
{
    public Guid Id { get; set; }
}
