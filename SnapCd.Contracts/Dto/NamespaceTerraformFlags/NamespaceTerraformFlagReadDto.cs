using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.NamespaceTerraformFlags;

public class NamespaceTerraformFlagReadDto : NamespaceTerraformFlagCreateDto, IDto
{
    public Guid Id { get; set; }
}
