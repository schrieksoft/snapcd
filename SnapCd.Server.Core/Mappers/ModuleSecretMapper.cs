using SnapCd.Contracts.Dto.Secrets.Scoped;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleSecretMapper
{
    public static ModuleSecret ToEntity(ModuleSecretDto dto, Guid organizationId)
    {
        return new ModuleSecret
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = dto.Name,
            ModuleId = dto.ModuleId
        };
    }

    public static ModuleSecretDto ToDto(ModuleSecret entity)
    {
        return new ModuleSecretDto
        {
            Id = entity.Id,
            Name = entity.Name,
            ModuleId = entity.ModuleId
        };
    }

    public static void UpdateEntity(ModuleSecret entity, ModuleSecretDto dto)
    {
        entity.Name = dto.Name;
        entity.ModuleId = dto.ModuleId;
    }
}