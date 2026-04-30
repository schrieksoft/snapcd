using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.NamespacePulumiFlags;

public class NamespacePulumiFlagReadDto : NamespacePulumiFlagCreateDto, IDto
{
    public Guid Id { get; set; }
}
