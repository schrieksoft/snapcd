using SnapCd.Contracts.Dto.ModuleInputs;

namespace SnapCd.Server.Core.Services.Crud.Interfaces;

public interface IModuleInputFromDefinitionService
{
    Task<ModuleInputFromDefinitionReadDto> Get(Guid moduleId, string name, Guid organizationId);
    Task<ModuleInputFromDefinitionReadDto> Get(Guid id, Guid organizationId);
    Task<ModuleInputFromDefinitionReadDto> Create(ModuleInputFromDefinitionCreateDto dto, Guid organizationId);
    Task<ModuleInputFromDefinitionReadDto> Update(ModuleInputFromDefinitionUpdateDto dto, Guid id, Guid organizationId);
    Task Delete(Guid id, Guid organizationId);
    Task<List<ModuleInputFromDefinitionReadDto>> ListByParentId(Guid parentId, Guid organizationId);
}