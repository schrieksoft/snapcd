using SnapCd.Contracts.Dto.GroupMembers;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;

namespace SnapCd.Server.Core.Mappers.GroupMembers;

public static class ServicePrincipalGroupMemberMapper
{
    public static ServicePrincipalGroupMember ToEntity(ServicePrincipalGroupMemberCreateDto dto, Guid organizationId)
    {
        return new ServicePrincipalGroupMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            GroupId = dto.GroupId,
            ServicePrincipalId = dto.ServicePrincipalId
        };
    }

    public static ServicePrincipalGroupMemberReadDto ToDto(ServicePrincipalGroupMember entity)
    {
        return new ServicePrincipalGroupMemberReadDto
        {
            Id = entity.Id,
            GroupId = entity.GroupId,
            ServicePrincipalId = entity.ServicePrincipalId
        };
    }

    public static void UpdateEntity(ServicePrincipalGroupMember entity, ServicePrincipalGroupMemberUpdateDto dto)
    {
        entity.GroupId = dto.GroupId;
        entity.ServicePrincipalId = dto.ServicePrincipalId;
    }
}