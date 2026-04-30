using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.NamespaceTerraformArrayFlags;

public class NamespaceTerraformArrayFlagUpdateDto : NamespaceTerraformArrayFlagCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
