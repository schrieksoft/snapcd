using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.NamespaceInputs;

public class NamespaceInputFromSecretUpdateDto : NamespaceInputFromSecretCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
