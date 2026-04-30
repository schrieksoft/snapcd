using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.Namespaces;

public class NamespaceReadDto : NamespaceCreateDto, IDto
{
    public Guid Id { get; set; }
}