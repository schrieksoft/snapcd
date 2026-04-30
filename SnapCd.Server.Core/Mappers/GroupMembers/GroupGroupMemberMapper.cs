using SnapCd.Contracts.Dto.GroupMembers;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;

namespace SnapCd.Server.Core.Mappers.GroupMembers;

public static class GroupGroupMemberMapper
{
    public static GroupGroupMember ToEntity(GroupGroupMemberCreateDto dto, Guid organizationId)
    {
        return new GroupGroupMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            GroupId = dto.GroupId,
            MemberGroupId = dto.MemberGroupId
        };
    }

    public static GroupGroupMemberReadDto ToDto(GroupGroupMember entity)
    {
        return new GroupGroupMemberReadDto
        {
            Id = entity.Id,
            GroupId = entity.GroupId,
            MemberGroupId = entity.MemberGroupId
        };
    }

    public static void UpdateEntity(GroupGroupMember entity, GroupGroupMemberUpdateDto dto)
    {
        entity.GroupId = dto.GroupId;
        entity.MemberGroupId = dto.MemberGroupId;
    }
}