using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.NamespaceHooks;

public class NamespaceHookUpdateDto : NamespaceHookCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
