namespace SnapCd.Server.Core.Entities.Definition.GroupMembers;

public class UserGroupMember : GroupMember
{
    public Guid UserId { get; set; }

    public OrganizationUser OrganizationUser { get; set; } = null!;
}