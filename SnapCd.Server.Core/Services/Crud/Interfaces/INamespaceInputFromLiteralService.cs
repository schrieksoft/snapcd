using SnapCd.Contracts.Dto.NamespaceInputs;

namespace SnapCd.Server.Core.Services.Crud.Interfaces;

public interface INamespaceInputFromLiteralService
{
    Task<NamespaceInputFromLiteralReadDto> Get(Guid namespaceId, string name, Guid organizationId);
    Task<NamespaceInputFromLiteralReadDto> Get(Guid id, Guid organizationId);
    Task<NamespaceInputFromLiteralReadDto> Create(NamespaceInputFromLiteralCreateDto dto, Guid organizationId);
    Task<NamespaceInputFromLiteralReadDto> Update(NamespaceInputFromLiteralUpdateDto dto, Guid id, Guid organizationId);
    Task Delete(Guid id, Guid organizationId);
    Task<List<NamespaceInputFromLiteralReadDto>> ListByParentId(Guid parentId, Guid organizationId);
}