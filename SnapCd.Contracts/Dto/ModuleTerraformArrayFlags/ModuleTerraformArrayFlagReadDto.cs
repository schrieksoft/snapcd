using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleTerraformArrayFlags;

public class ModuleTerraformArrayFlagReadDto : ModuleTerraformArrayFlagCreateDto, IDto
{
    public Guid Id { get; set; }
}
