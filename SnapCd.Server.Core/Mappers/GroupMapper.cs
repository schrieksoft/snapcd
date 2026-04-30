using SnapCd.Contracts.Dto.Groups;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class GroupMapper
{
    public static Group ToEntity(GroupCreateDto dto, Guid organizationId)
    {
        return new Group
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            Name = dto.Name,
            Description = dto.Description
        };
    }

    public static GroupReadDto ToDto(Group entity)
    {
        return new GroupReadDto
        {
            Id = entity.Id,
            Name = entity.Name,
            Description = entity.Description
        };
    }

    public static void UpdateEntity(Group entity, GroupUpdateDto dto)
    {
        entity.Name = dto.Name;
        entity.Description = dto.Description;
    }
}