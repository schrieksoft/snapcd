using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.NamespaceHooks;

public class NamespaceHookReadDto : NamespaceHookCreateDto, IDto
{
    public Guid Id { get; set; }
}
