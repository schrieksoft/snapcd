using SnapCd.Contracts.Dto.NamespaceInputs;

namespace SnapCd.Server.Core.Services.Crud.Interfaces;

public interface INamespaceInputFromSecretService
{
    Task<NamespaceInputFromSecretReadDto> Get(Guid namespaceId, string name, Guid organizationId);
    Task<NamespaceInputFromSecretReadDto> Get(Guid id, Guid organizationId);
    Task<NamespaceInputFromSecretReadDto> Create(NamespaceInputFromSecretCreateDto dto, Guid organizationId);
    Task<NamespaceInputFromSecretReadDto> Update(NamespaceInputFromSecretUpdateDto dto, Guid id, Guid organizationId);
    Task Delete(Guid id, Guid organizationId);
    Task<List<NamespaceInputFromSecretReadDto>> ListByParentId(Guid parentId, Guid organizationId);
}