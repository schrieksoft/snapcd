using SnapCd.Contracts.Dto.ModuleInputs;

namespace SnapCd.Server.Core.Services.Crud.Interfaces;

public interface IModuleInputFromNamespaceService
{
    Task<ModuleInputFromNamespaceReadDto> Get(Guid moduleId, string name, Guid organizationId);
    Task<ModuleInputFromNamespaceReadDto> Get(Guid id, Guid organizationId);
    Task<ModuleInputFromNamespaceReadDto> Create(ModuleInputFromNamespaceCreateDto dto, Guid organizationId);
    Task<ModuleInputFromNamespaceReadDto> Update(ModuleInputFromNamespaceUpdateDto dto, Guid id, Guid organizationId);
    Task Delete(Guid id, Guid organizationId);
    Task<List<ModuleInputFromNamespaceReadDto>> ListByParentId(Guid parentId, Guid organizationId);
}