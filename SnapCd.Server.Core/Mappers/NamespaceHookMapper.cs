using SnapCd.Contracts.Dto.NamespaceHooks;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class NamespaceHookMapper
{
    public static NamespaceHook ToEntity(NamespaceHookCreateDto dto, Guid organizationId)
    {
        return new NamespaceHook
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Task = dto.Task,
            Phase = dto.Phase,
            Script = dto.Script,
            NamespaceId = dto.NamespaceId
        };
    }

    public static NamespaceHookReadDto ToDto(NamespaceHook entity)
    {
        return new NamespaceHookReadDto
        {
            Id = entity.Id,
            Task = entity.Task,
            Phase = entity.Phase,
            Script = entity.Script,
            NamespaceId = entity.NamespaceId
        };
    }

    public static void UpdateEntity(NamespaceHook entity, NamespaceHookUpdateDto dto)
    {
        entity.Task = dto.Task;
        entity.Phase = dto.Phase;
        entity.Script = dto.Script;
        entity.NamespaceId = dto.NamespaceId;
    }
}
