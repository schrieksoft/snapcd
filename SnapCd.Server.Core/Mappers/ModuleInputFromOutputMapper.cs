using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleInputFromOutputMapper
{
    public static TEntity ToEntity<TEntity>(ModuleInputFromOutputCreateDto dto, Guid organizationId)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInputFromOutput, new()
    {
        return new TEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            Name = dto.Name,
            OutputModuleId = dto.OutputModuleId,
            OutputName = dto.OutputName
        };
    }

    public static ModuleInputFromOutputDtoRead ToDto<TEntity>(TEntity entity)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInputFromOutput
    {
        return new ModuleInputFromOutputDtoRead
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            Name = entity.Name,
            InputKind = entity.InputKind,
            OutputModuleId = entity.OutputModuleId,
            OutputName = entity.OutputName
        };
    }

    public static void UpdateEntity<TEntity>(TEntity entity, ModuleInputFromOutputUpdateDto dto)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInputFromOutput
    {
        entity.ModuleId = dto.ModuleId;
        entity.Name = dto.Name;
        entity.OutputModuleId = dto.OutputModuleId;
        entity.OutputName = dto.OutputName;
    }
}