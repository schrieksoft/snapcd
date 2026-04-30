using SnapCd.Contracts.Dto.NamespaceInputs.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Mappers;

public static class NamespaceInputMapper
{
    public static TEntity ToEntity<TEntity>(NamespaceInputCreateDto dto, Guid organizationId)
        where TEntity : Entities.Definition.Base.NamespaceInput, INamespaceInput, new()
    {
        return new TEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            NamespaceId = dto.NamespaceId,
            Name = dto.Name,
            UsageMode = dto.UsageMode
        };
    }

    public static NamespaceInputReadDto ToDto<TEntity>(TEntity entity)
        where TEntity : Entities.Definition.Base.NamespaceInput, INamespaceInput
    {
        return new NamespaceInputReadDto
        {
            Id = entity.Id,
            NamespaceId = entity.NamespaceId,
            Name = entity.Name,
            UsageMode = entity.UsageMode,
            InputKind = entity.InputKind
        };
    }

    public static void UpdateEntity<TEntity>(TEntity entity, NamespaceInputUpdateDto dto)
        where TEntity : Entities.Definition.Base.NamespaceInput, INamespaceInput
    {
        entity.NamespaceId = dto.NamespaceId;
        entity.Name = dto.Name;
        entity.UsageMode = dto.UsageMode;
    }
}