using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.NamespacePulumiFlags;

public class NamespacePulumiFlagUpdateDto : NamespacePulumiFlagCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
