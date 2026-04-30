using SnapCd.Contracts.Dto.NamespaceBackendConfigs;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class NamespaceBackendConfigMapper
{
    public static NamespaceBackendConfig ToEntity(NamespaceBackendConfigCreateDto dto, Guid organizationId)
    {
        return new NamespaceBackendConfig
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = dto.Name,
            Value = dto.Value,
            NamespaceId = dto.NamespaceId
        };
    }

    public static NamespaceBackendConfigReadDto ToDto(NamespaceBackendConfig entity)
    {
        return new NamespaceBackendConfigReadDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Value = entity.Value,
            NamespaceId = entity.NamespaceId
        };
    }

    public static void UpdateEntity(NamespaceBackendConfig entity, NamespaceBackendConfigUpdateDto dto)
    {
        entity.Name = dto.Name;
        entity.Value = dto.Value;
        entity.NamespaceId = dto.NamespaceId;
    }
}