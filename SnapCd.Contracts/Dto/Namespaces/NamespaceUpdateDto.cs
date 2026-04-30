using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.Namespaces;

public class NamespaceUpdateDto : NamespaceCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}