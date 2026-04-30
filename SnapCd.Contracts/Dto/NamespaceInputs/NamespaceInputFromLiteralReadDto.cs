using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.NamespaceInputs;

public class NamespaceInputFromLiteralReadDto : NamespaceInputFromLiteralCreateDto, IDto
{
    public Guid Id { get; set; }
}