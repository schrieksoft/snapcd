using SnapCd.Contracts.Dto.ModuleInputs;

namespace SnapCd.Server.Core.Services.Crud.Interfaces;

public interface IModuleInputFromOutputService
{
    Task<ModuleInputFromOutputDtoRead> Get(Guid moduleId, string name, Guid organizationId);
    Task<ModuleInputFromOutputDtoRead> Get(Guid id, Guid organizationId);
    Task<ModuleInputFromOutputDtoRead> Create(ModuleInputFromOutputCreateDto dto, Guid organizationId);
    Task<ModuleInputFromOutputDtoRead> Update(ModuleInputFromOutputUpdateDto dto, Guid id, Guid organizationId);
    Task Delete(Guid id, Guid organizationId);
    Task<List<ModuleInputFromOutputDtoRead>> ListByParentId(Guid parentId, Guid organizationId);
}