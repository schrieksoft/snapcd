using SnapCd.Contracts.Dto.NamespaceInputs;

namespace SnapCd.Server.Core.Services.Crud.Interfaces;

public interface INamespaceInputFromDefinitionService
{
    Task<NamespaceInputFromDefinitionReadDto> Get(Guid namespaceId, string name, Guid organizationId);
    Task<NamespaceInputFromDefinitionReadDto> Get(Guid id, Guid organizationId);
    Task<NamespaceInputFromDefinitionReadDto> Create(NamespaceInputFromDefinitionCreateDto dto, Guid organizationId);
    Task<NamespaceInputFromDefinitionReadDto> Update(NamespaceInputFromDefinitionUpdateDto dto, Guid id, Guid organizationId);
    Task Delete(Guid id, Guid organizationId);
    Task<List<NamespaceInputFromDefinitionReadDto>> ListByParentId(Guid parentId, Guid organizationId);
}