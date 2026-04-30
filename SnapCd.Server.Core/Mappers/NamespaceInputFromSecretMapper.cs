using SnapCd.Contracts.Dto.NamespaceInputs;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Mappers;

public static class NamespaceInputFromSecretMapper
{
    public static TEntity ToEntity<TEntity>(NamespaceInputFromSecretCreateDto dto, Guid organizationId)
        where TEntity : NamespaceInputWithType, INamespaceInputFromSecret, new()
    {
        return new TEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            NamespaceId = dto.NamespaceId,
            Name = dto.Name,
            UsageMode = dto.UsageMode,
            Type = dto.Type,
            SecretId = dto.SecretId
        };
    }

    public static NamespaceInputFromSecretReadDto ToDto<TEntity>(TEntity entity)
        where TEntity : NamespaceInputWithType, INamespaceInputFromSecret
    {
        return new NamespaceInputFromSecretReadDto
        {
            Id = entity.Id,
            NamespaceId = entity.NamespaceId,
            Name = entity.Name,
            UsageMode = entity.UsageMode,
            InputKind = entity.InputKind,
            Type = entity.Type,
            SecretId = entity.SecretId
        };
    }

    public static void UpdateEntity<TEntity>(TEntity entity, NamespaceInputFromSecretUpdateDto dto)
        where TEntity : NamespaceInputWithType, INamespaceInputFromSecret
    {
        entity.NamespaceId = dto.NamespaceId;
        entity.Name = dto.Name;
        entity.UsageMode = dto.UsageMode;
        entity.Type = dto.Type;
        entity.SecretId = dto.SecretId;
    }
}