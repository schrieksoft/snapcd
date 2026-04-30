using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.NamespacePulumiArrayFlags;

public class NamespacePulumiArrayFlagReadDto : NamespacePulumiArrayFlagCreateDto, IDto
{
    public Guid Id { get; set; }
}
