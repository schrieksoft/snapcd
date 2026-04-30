using SnapCd.Contracts.Dto.ModuleTerraformFlags;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleTerraformFlagMapper
{
    public static ModuleTerraformFlag ToEntity(ModuleTerraformFlagCreateDto dto, Guid organizationId)
    {
        return new ModuleTerraformFlag
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Task = dto.Task,
            Flag = dto.Flag,
            Value = dto.Value,
            ModuleId = dto.ModuleId
        };
    }

    public static ModuleTerraformFlagReadDto ToDto(ModuleTerraformFlag entity)
    {
        return new ModuleTerraformFlagReadDto
        {
            Id = entity.Id,
            Task = entity.Task,
            Flag = entity.Flag,
            Value = entity.Value,
            ModuleId = entity.ModuleId
        };
    }

    public static void UpdateEntity(ModuleTerraformFlag entity, ModuleTerraformFlagUpdateDto dto)
    {
        entity.Task = dto.Task;
        entity.Flag = dto.Flag;
        entity.Value = dto.Value;
        entity.ModuleId = dto.ModuleId;
    }
}
