using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.NamespaceTerraformArrayFlags;

public class NamespaceTerraformArrayFlagReadDto : NamespaceTerraformArrayFlagCreateDto, IDto
{
    public Guid Id { get; set; }
}
