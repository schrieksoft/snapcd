using SnapCd.Contracts;
using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Secured;

namespace SnapCd.Server.Core.Services.Crud;

public class ModuleInputFromOutputBaseService
{
    private readonly ModuleInputSecuredRepository _repo;

    public ModuleInputFromOutputBaseService(
        ModuleInputSecuredRepository repo)
    {
        _repo = repo;
    }

    public async Task<ModuleInputFromOutputDtoRead> Get(Guid id, Guid organizationId)
    {
        var entity = await _repo.Get(id, organizationId);
        var dto = entity switch
        {
            ModuleParamFromOutput param => ModuleInputFromOutputMapper.ToDto(param),
            ModuleEnvVarFromOutput envVar => ModuleInputFromOutputMapper.ToDto(envVar),
            _ => throw new InvalidOperationException($"Unknown entity type: {entity.GetType().Name}")
        };

        dto.InputKind = entity switch
        {
            ModuleParamFromOutput => InputKind.Param,
            ModuleEnvVarFromOutput => InputKind.EnvVar,
            _ => throw new InvalidOperationException($"Unknown entity type: {entity.GetType().Name}")
        };

        return dto;
    }

    public async Task<ModuleInputFromOutputDtoRead> Get(Guid moduleId, string name, Guid organizationId)
    {
        var entity = await _repo.Get(moduleId, name, organizationId);
        var dto = entity switch
        {
            ModuleParamFromOutput param => ModuleInputFromOutputMapper.ToDto(param),
            ModuleEnvVarFromOutput envVar => ModuleInputFromOutputMapper.ToDto(envVar),
            _ => throw new InvalidOperationException($"Unknown entity type: {entity.GetType().Name}")
        };

        dto.InputKind = entity switch
        {
            ModuleParamFromOutput => InputKind.Param,
            ModuleEnvVarFromOutput => InputKind.EnvVar,
            _ => throw new InvalidOperationException($"Unknown entity type: {entity.GetType().Name}")
        };

        return dto;
    }

    public async Task Delete(Guid id, Guid organizationId)
    {
        await _repo.Delete(id, organizationId);
    }
}