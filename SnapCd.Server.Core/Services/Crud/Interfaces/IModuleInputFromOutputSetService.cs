using SnapCd.Contracts.Dto.ModuleInputs;

namespace SnapCd.Server.Core.Services.Crud.Interfaces;

public interface IModuleInputFromOutputSetService
{
    Task<ModuleInputFromOutputSetReadDto> Get(Guid moduleId, string name, Guid organizationId);
    Task<ModuleInputFromOutputSetReadDto> Get(Guid id, Guid organizationId);
    Task<ModuleInputFromOutputSetReadDto> Create(ModuleInputFromOutputSetCreateDto dto, Guid organizationId);
    Task<ModuleInputFromOutputSetReadDto> Update(ModuleInputFromOutputSetUpdateDto dto, Guid id, Guid organizationId);
    Task Delete(Guid id, Guid organizationId);
    Task<List<ModuleInputFromOutputSetReadDto>> ListByParentId(Guid parentId, Guid organizationId);
}