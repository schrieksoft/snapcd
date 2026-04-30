using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.NamespaceInputs;

public class NamespaceInputFromLiteralUpdateDto : NamespaceInputFromLiteralCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
