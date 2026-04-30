using SnapCd.Contracts.Dto.Stacks;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class StackMapper
{
    public static Stack ToEntity(StackCreateDto dto, Guid organizationId)
    {
        return new Stack
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = dto.Name,
            TriggerBehaviourOnModified = dto.TriggerBehaviourOnModified
        };
    }

    public static StackReadDto ToDto(Stack entity)
    {
        return new StackReadDto
        {
            Id = entity.Id,
            Name = entity.Name,
            TriggerBehaviourOnModified = entity.TriggerBehaviourOnModified
        };
    }

    public static void UpdateEntity(Stack entity, StackUpdateDto dto)
    {
        entity.Name = dto.Name;
        entity.TriggerBehaviourOnModified = dto.TriggerBehaviourOnModified;
    }
}