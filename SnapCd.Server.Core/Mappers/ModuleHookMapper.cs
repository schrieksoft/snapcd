using SnapCd.Contracts.Dto.ModuleHooks;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class ModuleHookMapper
{
    public static ModuleHook ToEntity(ModuleHookCreateDto dto, Guid organizationId)
    {
        return new ModuleHook
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Task = dto.Task,
            Phase = dto.Phase,
            Script = dto.Script,
            ModuleId = dto.ModuleId
        };
    }

    public static ModuleHookReadDto ToDto(ModuleHook entity)
    {
        return new ModuleHookReadDto
        {
            Id = entity.Id,
            Task = entity.Task,
            Phase = entity.Phase,
            Script = entity.Script,
            ModuleId = entity.ModuleId
        };
    }

    public static void UpdateEntity(ModuleHook entity, ModuleHookUpdateDto dto)
    {
        entity.Task = dto.Task;
        entity.Phase = dto.Phase;
        entity.Script = dto.Script;
        entity.ModuleId = dto.ModuleId;
    }
}
