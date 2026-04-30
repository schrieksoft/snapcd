using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleInputFromDefinitionMapper
{
    public static TEntity ToEntity<TEntity>(ModuleInputFromDefinitionCreateDto dto, Guid organizationId)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInputFromDefinition, new()
    {
        return new TEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            Name = dto.Name,
            DefinitionName = dto.DefinitionName
        };
    }

    public static ModuleInputFromDefinitionReadDto ToDto<TEntity>(TEntity entity)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInputFromDefinition
    {
        return new ModuleInputFromDefinitionReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            Name = entity.Name,
            InputKind = entity.InputKind,
            DefinitionName = entity.DefinitionName
        };
    }

    public static void UpdateEntity<TEntity>(TEntity entity, ModuleInputFromDefinitionUpdateDto dto)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInputFromDefinition
    {
        entity.ModuleId = dto.ModuleId;
        entity.Name = dto.Name;
        entity.DefinitionName = dto.DefinitionName;
    }
}