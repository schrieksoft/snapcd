using SnapCd.Contracts.Dto.GroupMembers;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;

namespace SnapCd.Server.Core.Mappers.GroupMembers;

public static class UserGroupMemberMapper
{
    public static UserGroupMember ToEntity(UserGroupMemberCreateDto dto, Guid organizationId)
    {
        return new UserGroupMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            GroupId = dto.GroupId,
            UserId = dto.UserId
        };
    }

    public static UserGroupMemberReadDto ToDto(UserGroupMember entity)
    {
        return new UserGroupMemberReadDto
        {
            Id = entity.Id,
            GroupId = entity.GroupId,
            UserId = entity.UserId
        };
    }

    public static void UpdateEntity(UserGroupMember entity, UserGroupMemberUpdateDto dto)
    {
        entity.GroupId = dto.GroupId;
        entity.UserId = dto.UserId;
    }
}