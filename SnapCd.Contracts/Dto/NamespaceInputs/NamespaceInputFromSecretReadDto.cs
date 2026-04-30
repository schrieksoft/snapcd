using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.NamespaceInputs;

public class NamespaceInputFromSecretReadDto : NamespaceInputFromSecretCreateDto, IDto
{
    public Guid Id { get; set; }
}