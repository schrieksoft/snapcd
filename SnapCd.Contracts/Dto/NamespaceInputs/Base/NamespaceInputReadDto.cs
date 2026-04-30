using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.NamespaceInputs.Base;

public class NamespaceInputReadDto : NamespaceInputCreateDto, IDto
{
    public Guid Id { get; set; }
}