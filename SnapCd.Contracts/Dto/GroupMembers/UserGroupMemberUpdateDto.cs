using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.GroupMembers;

public class UserGroupMemberUpdateDto : UserGroupMemberCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
