using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleTerraformFlags;

public class ModuleTerraformFlagReadDto : ModuleTerraformFlagCreateDto, IDto
{
    public Guid Id { get; set; }
}
