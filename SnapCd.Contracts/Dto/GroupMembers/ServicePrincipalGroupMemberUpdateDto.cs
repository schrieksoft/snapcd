using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.GroupMembers;

public class ServicePrincipalGroupMemberUpdateDto : ServicePrincipalGroupMemberCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
