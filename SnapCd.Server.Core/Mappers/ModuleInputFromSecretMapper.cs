using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleInputFromSecretMapper
{
    public static TEntity ToEntity<TEntity>(ModuleInputFromSecretCreateDto dto, Guid organizationId)
        where TEntity : Entities.Definition.Base.ModuleInputWithType, IModuleInputFromSecret, new()
    {
        return new TEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            Name = dto.Name,
            SecretId = dto.SecretId,
            Type = dto.Type
        };
    }

    public static ModuleInputFromSecretReadDto ToDto<TEntity>(TEntity entity)
        where TEntity : Entities.Definition.Base.ModuleInputWithType, IModuleInputFromSecret
    {
        return new ModuleInputFromSecretReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            Name = entity.Name,
            InputKind = entity.InputKind,
            SecretId = entity.SecretId,
            Type = entity.Type
        };
    }

    public static void UpdateEntity<TEntity>(TEntity entity, ModuleInputFromSecretUpdateDto dto)
        where TEntity : Entities.Definition.Base.ModuleInputWithType, IModuleInputFromSecret
    {
        entity.ModuleId = dto.ModuleId;
        entity.Name = dto.Name;
        entity.SecretId = dto.SecretId;
        entity.Type = dto.Type;
    }
}