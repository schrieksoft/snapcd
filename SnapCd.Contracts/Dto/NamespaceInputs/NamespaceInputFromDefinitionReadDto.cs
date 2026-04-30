using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.NamespaceInputs;

public class NamespaceInputFromDefinitionReadDto : NamespaceInputFromDefinitionCreateDto, IDto
{
    public Guid Id { get; set; }
}