namespace SnapCd.Contracts.Dto.GroupMembers;

public class GroupGroupMemberCreateDto
{
    public Guid GroupId { get; set; }
    public Guid MemberGroupId { get; set; }
}
