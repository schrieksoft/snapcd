using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleTerraformArrayFlags;

public class ModuleTerraformArrayFlagUpdateDto : ModuleTerraformArrayFlagCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
