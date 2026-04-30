namespace SnapCd.Server.Core.Entities.Definition.GroupMembers;

public class GroupGroupMember : GroupMember
{
    public Guid MemberGroupId { get; set; }

    public Group MemberGroup { get; set; } = null!;
}