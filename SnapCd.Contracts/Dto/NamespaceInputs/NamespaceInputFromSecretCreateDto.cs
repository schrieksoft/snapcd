using SnapCd.Contracts.Dto.NamespaceInputs.Base;

namespace SnapCd.Contracts.Dto.NamespaceInputs;

public class NamespaceInputFromSecretCreateDto : NamespaceInputCreateDto
{
    public Guid SecretId { get; set; }

    public InputType Type { get; set; }
}
