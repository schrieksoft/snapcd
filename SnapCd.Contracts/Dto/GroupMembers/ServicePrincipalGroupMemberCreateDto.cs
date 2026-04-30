namespace SnapCd.Contracts.Dto.GroupMembers;

public class ServicePrincipalGroupMemberCreateDto
{
    public Guid GroupId { get; set; }
    public Guid ServicePrincipalId { get; set; }
}
