using SnapCd.Contracts.Dto.ModuleInputs;

namespace SnapCd.Server.Core.Services.Crud.Interfaces;

public interface IModuleInputFromSecretService
{
    Task<ModuleInputFromSecretReadDto> Get(Guid moduleId, string name, Guid organizationId);
    Task<ModuleInputFromSecretReadDto> Get(Guid id, Guid organizationId);
    Task<ModuleInputFromSecretReadDto> Create(ModuleInputFromSecretCreateDto dto, Guid organizationId);
    Task<ModuleInputFromSecretReadDto> Update(ModuleInputFromSecretUpdateDto dto, Guid id, Guid organizationId);
    Task Delete(Guid id, Guid organizationId);
    Task<List<ModuleInputFromSecretReadDto>> ListByParentId(Guid parentId, Guid organizationId);
}