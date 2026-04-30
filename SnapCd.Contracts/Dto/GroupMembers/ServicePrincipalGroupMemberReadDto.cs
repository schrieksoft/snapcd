using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.GroupMembers;

public class ServicePrincipalGroupMemberReadDto : ServicePrincipalGroupMemberCreateDto, IDto
{
    public Guid Id { get; set; }
}