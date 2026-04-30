namespace SnapCd.Contracts.Dto.GroupMembers.Base;

public class GroupMemberCreateDto
{
    public Guid GroupId { get; set; }

    public Guid PrincipalId { get; set; }

    public GroupMemberDiscriminator GroupMemberDiscriminator { get; set; }
}
