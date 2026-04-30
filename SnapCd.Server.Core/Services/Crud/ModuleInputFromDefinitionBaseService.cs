using SnapCd.Contracts;
using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Secured;

namespace SnapCd.Server.Core.Services.Crud;

public class ModuleInputFromDefinitionBaseService
{
    private readonly ModuleInputSecuredRepository _repo;

    public ModuleInputFromDefinitionBaseService(
        ModuleInputSecuredRepository repo)
    {
        _repo = repo;
    }


    public async Task<ModuleInputFromDefinitionReadDto> Get(Guid id, Guid organizationId)
    {
        var entity = await _repo.Get(id, organizationId);
        var dto = entity switch
        {
            ModuleParamFromDefinition param => ModuleInputFromDefinitionMapper.ToDto(param),
            ModuleEnvVarFromDefinition envVar => ModuleInputFromDefinitionMapper.ToDto(envVar),
            _ => throw new InvalidOperationException($"Unknown entity type: {entity.GetType().Name}")
        };

        dto.InputKind = entity switch
        {
            ModuleParamFromDefinition => InputKind.Param,
            ModuleEnvVarFromDefinition => InputKind.EnvVar,
            _ => throw new InvalidOperationException($"Unknown entity type: {entity.GetType().Name}")
        };

        return dto;
    }

    public async Task<ModuleInputFromDefinitionReadDto> Get(Guid moduleId, string name, Guid organizationId)
    {
        var entity = await _repo.Get(moduleId, name, organizationId);
        var dto = entity switch
        {
            ModuleParamFromDefinition param => ModuleInputFromDefinitionMapper.ToDto(param),
            ModuleEnvVarFromDefinition envVar => ModuleInputFromDefinitionMapper.ToDto(envVar),
            _ => throw new InvalidOperationException($"Unknown entity type: {entity.GetType().Name}")
        };

        dto.InputKind = entity switch
        {
            ModuleParamFromDefinition => InputKind.Param,
            ModuleEnvVarFromDefinition => InputKind.EnvVar,
            _ => throw new InvalidOperationException($"Unknown entity type: {entity.GetType().Name}")
        };

        return dto;
    }

    public async Task Delete(Guid id, Guid organizationId)
    {
        await _repo.Delete(id, organizationId);
    }
}