using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.NamespaceInputs.Base;

public class NamespaceInputUpdateDto : NamespaceInputCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
