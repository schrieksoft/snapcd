using SnapCd.Contracts.Dto.ModuleInputs;

namespace SnapCd.Server.Core.Services.Crud.Interfaces;

public interface IModuleInputFromLiteralService
{
    Task<ModuleInputFromLiteralReadDto> Get(Guid moduleId, string name, Guid organizationId);
    Task<ModuleInputFromLiteralReadDto> Get(Guid id, Guid organizationId);
    Task<ModuleInputFromLiteralReadDto> Create(ModuleInputFromLiteralCreateDto dto, Guid organizationId);
    Task<ModuleInputFromLiteralReadDto> Update(ModuleInputFromLiteralUpdateDto dto, Guid id, Guid organizationId);
    Task Delete(Guid id, Guid organizationId);
    Task<List<ModuleInputFromLiteralReadDto>> ListByParentId(Guid parentId, Guid organizationId);
}