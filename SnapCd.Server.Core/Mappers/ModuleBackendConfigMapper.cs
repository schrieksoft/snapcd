using SnapCd.Contracts.Dto.ModuleBackendConfigs;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleBackendConfigMapper
{
    public static ModuleBackendConfig ToEntity(ModuleBackendConfigCreateDto dto, Guid organizationId)
    {
        return new ModuleBackendConfig
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = dto.Name,
            Value = dto.Value,
            ModuleId = dto.ModuleId
        };
    }

    public static ModuleBackendConfigReadDto ToDto(ModuleBackendConfig entity)
    {
        return new ModuleBackendConfigReadDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Value = entity.Value,
            ModuleId = entity.ModuleId
        };
    }

    public static void UpdateEntity(ModuleBackendConfig entity, ModuleBackendConfigUpdateDto dto)
    {
        entity.Name = dto.Name;
        entity.Value = dto.Value;
        entity.ModuleId = dto.ModuleId;
    }
}