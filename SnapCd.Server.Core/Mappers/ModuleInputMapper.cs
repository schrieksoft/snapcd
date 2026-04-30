using SnapCd.Contracts.Dto.ModuleInputs.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleInputMapper
{
    public static TEntity ToEntity<TEntity>(ModuleInputCreateDto dto, Guid organizationId)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInput, new()
    {
        return new TEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            Name = dto.Name,
            InputKind = dto.InputKind
        };
    }

    public static ModuleInputReadDto ToDto<TEntity>(TEntity entity)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInput
    {
        return new ModuleInputReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            Name = entity.Name,
            InputKind = entity.InputKind
        };
    }

    public static void UpdateEntity<TEntity>(TEntity entity, ModuleInputUpdateDto dto)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInput
    {
        entity.ModuleId = dto.ModuleId;
        entity.Name = dto.Name;
    }
}