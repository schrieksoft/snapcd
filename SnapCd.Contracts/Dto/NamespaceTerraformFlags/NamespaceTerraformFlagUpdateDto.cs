using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.NamespaceTerraformFlags;

public class NamespaceTerraformFlagUpdateDto : NamespaceTerraformFlagCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
