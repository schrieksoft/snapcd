using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Entities.Interfaces;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleInputFromLiteralMapper
{
    public static TEntity ToEntity<TEntity>(ModuleInputFromLiteralCreateDto dto, Guid organizationId)
        where TEntity : Entities.Definition.Base.ModuleInputWithType, IModuleInputFromLiteral, new()
    {
        return new TEntity
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ModuleId = dto.ModuleId,
            Name = dto.Name,
            LiteralValue = dto.LiteralValue,
            Type = dto.Type
        };
    }

    public static ModuleInputFromLiteralReadDto ToDto<TEntity>(TEntity entity)
        where TEntity : Entities.Definition.Base.ModuleInputWithType, IModuleInputFromLiteral
    {
        return new ModuleInputFromLiteralReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            Name = entity.Name,
            InputKind = entity.InputKind,
            LiteralValue = entity.LiteralValue,
            Type = entity.Type
        };
    }

    public static void UpdateEntity<TEntity>(TEntity entity, ModuleInputFromLiteralUpdateDto dto)
        where TEntity : Entities.Definition.Base.ModuleInputWithType, IModuleInputFromLiteral
    {
        entity.ModuleId = dto.ModuleId;
        entity.Name = dto.Name;
        entity.LiteralValue = dto.LiteralValue;
        entity.Type = dto.Type;
    }
}