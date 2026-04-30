using SnapCd.Contracts.Dto.NamespaceInputs;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Mappers;

public static class NamespaceInputFromDefinitionMapper
{
    public static TEntity ToEntity<TEntity>(NamespaceInputFromDefinitionCreateDto dto, Guid organizationId)
        where TEntity : Entities.Definition.Base.NamespaceInput, INamespaceInputFromDefinition, new()
    {
        return new TEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            NamespaceId = dto.NamespaceId,
            Name = dto.Name,
            UsageMode = dto.UsageMode,
            DefinitionName = dto.DefinitionName
        };
    }

    public static NamespaceInputFromDefinitionReadDto ToDto<TEntity>(TEntity entity)
        where TEntity : Entities.Definition.Base.NamespaceInput, INamespaceInputFromDefinition
    {
        return new NamespaceInputFromDefinitionReadDto
        {
            Id = entity.Id,
            NamespaceId = entity.NamespaceId,
            Name = entity.Name,
            UsageMode = entity.UsageMode,
            InputKind = entity.InputKind,
            DefinitionName = entity.DefinitionName
        };
    }

    public static void UpdateEntity<TEntity>(TEntity entity, NamespaceInputFromDefinitionUpdateDto dto)
        where TEntity : Entities.Definition.Base.NamespaceInput, INamespaceInputFromDefinition
    {
        entity.NamespaceId = dto.NamespaceId;
        entity.Name = dto.Name;
        entity.UsageMode = dto.UsageMode;
        entity.DefinitionName = dto.DefinitionName;
    }
}