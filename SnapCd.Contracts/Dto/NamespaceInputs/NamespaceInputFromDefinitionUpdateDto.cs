using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.NamespaceInputs;

public class NamespaceInputFromDefinitionUpdateDto : NamespaceInputFromDefinitionCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
