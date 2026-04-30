using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleTerraformFlags;

public class ModuleTerraformFlagUpdateDto : ModuleTerraformFlagCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
