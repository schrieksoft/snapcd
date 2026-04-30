using SnapCd.Contracts.Dto.ModuleInputs.Base;

namespace SnapCd.Contracts.Dto.ModuleInputs;

public class ModuleInputFromSecretCreateDto : ModuleInputCreateDto
{
    public Guid SecretId { get; set; }

    public InputType Type { get; set; }
}
