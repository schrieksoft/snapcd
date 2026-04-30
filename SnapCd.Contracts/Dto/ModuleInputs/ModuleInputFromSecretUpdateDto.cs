using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.ModuleInputs;

public class ModuleInputFromSecretUpdateDto : ModuleInputFromSecretCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
