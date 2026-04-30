using SnapCd.Contracts;
using SnapCd.Contracts.Dto.GroupMembers.Base;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;

namespace SnapCd.Server.Core.Mappers.GroupMembers.Base;

public static class GroupMemberMapper
{
    public static GroupMember ToEntity(GroupMemberCreateDto dto, Guid organizationId)
    {
        var id = Guid.NewGuid();

        return dto.GroupMemberDiscriminator switch
        {
            GroupMemberDiscriminator.User => new UserGroupMember
            {
                Id = id,
                OrganizationId = organizationId,
                GroupId = dto.GroupId,
                UserId = dto.PrincipalId,
                GroupMemberDiscriminator = dto.GroupMemberDiscriminator
            },
            GroupMemberDiscriminator.ServicePrincipal => new ServicePrincipalGroupMember
            {
                Id = id,
                OrganizationId = organizationId,
                GroupId = dto.GroupId,
                ServicePrincipalId = dto.PrincipalId,
                GroupMemberDiscriminator = dto.GroupMemberDiscriminator
            },
            GroupMemberDiscriminator.Group => new GroupGroupMember
            {
                Id = id,
                OrganizationId = organizationId,
                GroupId = dto.GroupId,
                MemberGroupId = dto.PrincipalId,
                GroupMemberDiscriminator = dto.GroupMemberDiscriminator
            },
            _ => throw new ArgumentException($"Unknown GroupMemberDiscriminator: {dto.GroupMemberDiscriminator}")
        };
    }

    public static GroupMemberReadDto ToDto(GroupMember entity)
    {
        return new GroupMemberReadDto
        {
            Id = entity.Id,
            GroupId = entity.GroupId,
            PrincipalId = entity.PrincipalId,
            GroupMemberDiscriminator = entity.GroupMemberDiscriminator
        };
    }

    public static void UpdateEntity(GroupMember entity, GroupMemberUpdateDto dto)
    {
        entity.GroupId = dto.GroupId;
        entity.GroupMemberDiscriminator = dto.GroupMemberDiscriminator;

        // Update type-specific properties based on discriminator
        switch (entity)
        {
            case UserGroupMember userMember:
                userMember.UserId = dto.PrincipalId;
                break;
            case ServicePrincipalGroupMember spMember:
                spMember.ServicePrincipalId = dto.PrincipalId;
                break;
            case GroupGroupMember groupMember:
                groupMember.MemberGroupId = dto.PrincipalId;
                break;
        }
    }
}