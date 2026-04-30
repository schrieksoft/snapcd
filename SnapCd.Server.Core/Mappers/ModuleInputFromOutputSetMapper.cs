using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleInputFromOutputSetMapper
{
    public static TEntity ToEntity<TEntity>(ModuleInputFromOutputSetCreateDto dto, Guid organizationId)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInputFromOutputSet, new()
    {
        return new TEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            Name = dto.Name,
            OutputModuleId = dto.OutputModuleId
        };
    }

    public static ModuleInputFromOutputSetReadDto ToDto<TEntity>(TEntity entity)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInputFromOutputSet
    {
        return new ModuleInputFromOutputSetReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            Name = entity.Name,
            InputKind = entity.InputKind,
            OutputModuleId = entity.OutputModuleId
        };
    }

    public static void UpdateEntity<TEntity>(TEntity entity, ModuleInputFromOutputSetUpdateDto dto)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInputFromOutputSet
    {
        entity.ModuleId = dto.ModuleId;
        entity.Name = dto.Name;
        entity.OutputModuleId = dto.OutputModuleId;
    }
}