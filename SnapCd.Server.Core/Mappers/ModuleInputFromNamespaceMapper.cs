using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleInputFromNamespaceMapper
{
    public static TEntity ToEntity<TEntity>(ModuleInputFromNamespaceCreateDto dto, Guid organizationId)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInputFromNamespace, new()
    {
        return new TEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            Name = dto.Name,
            NamespaceInputId = dto.NamespaceInputId
        };
    }

    public static ModuleInputFromNamespaceReadDto ToDto<TEntity>(TEntity entity)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInputFromNamespace
    {
        return new ModuleInputFromNamespaceReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            Name = entity.Name,
            InputKind = entity.InputKind,
            NamespaceInputId = entity.NamespaceInputId
        };
    }

    public static void UpdateEntity<TEntity>(TEntity entity, ModuleInputFromNamespaceUpdateDto dto)
        where TEntity : Entities.Definition.Base.ModuleInput, IModuleInputFromNamespace
    {
        entity.ModuleId = dto.ModuleId;
        entity.Name = dto.Name;
        entity.NamespaceInputId = dto.NamespaceInputId;
    }
}