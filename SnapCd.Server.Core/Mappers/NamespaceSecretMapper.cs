using SnapCd.Contracts.Dto.Secrets.Scoped;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;

namespace SnapCd.Server.Core.Mappers;

public static class NamespaceSecretMapper
{
    public static NamespaceSecret ToEntity(NamespaceSecretDto dto, Guid organizationId)
    {
        return new NamespaceSecret
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = dto.Name,
            NamespaceId = dto.NamespaceId
        };
    }

    public static NamespaceSecretDto ToDto(NamespaceSecret entity)
    {
        return new NamespaceSecretDto
        {
            Id = entity.Id,
            Name = entity.Name,
            NamespaceId = entity.NamespaceId
        };
    }

    public static void UpdateEntity(NamespaceSecret entity, NamespaceSecretDto dto)
    {
        entity.Name = dto.Name;
        entity.NamespaceId = dto.NamespaceId;
    }
}