using SnapCd.Contracts.Dto.NamespaceInputs;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Mappers;

public static class NamespaceInputFromLiteralMapper
{
    public static TEntity ToEntity<TEntity>(NamespaceInputFromLiteralCreateDto dto, Guid organizationId)
        where TEntity : NamespaceInputWithType, INamespaceInputFromLiteral, new()
    {
        return new TEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            NamespaceId = dto.NamespaceId,
            Name = dto.Name,
            UsageMode = dto.UsageMode,
            Type = dto.Type,
            LiteralValue = dto.LiteralValue
        };
    }

    public static NamespaceInputFromLiteralReadDto ToDto<TEntity>(TEntity entity)
        where TEntity : NamespaceInputWithType, INamespaceInputFromLiteral
    {
        return new NamespaceInputFromLiteralReadDto
        {
            Id = entity.Id,
            NamespaceId = entity.NamespaceId,
            Name = entity.Name,
            UsageMode = entity.UsageMode,
            InputKind = entity.InputKind,
            Type = entity.Type,
            LiteralValue = entity.LiteralValue
        };
    }

    public static void UpdateEntity<TEntity>(TEntity entity, NamespaceInputFromLiteralUpdateDto dto)
        where TEntity : NamespaceInputWithType, INamespaceInputFromLiteral
    {
        entity.NamespaceId = dto.NamespaceId;
        entity.Name = dto.Name;
        entity.UsageMode = dto.UsageMode;
        entity.Type = dto.Type;
        entity.LiteralValue = dto.LiteralValue;
    }
}