using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.NamespacePulumiArrayFlags;

public class NamespacePulumiArrayFlagUpdateDto : NamespacePulumiArrayFlagCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
