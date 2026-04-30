using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.GroupMembers.Base;

public class GroupMemberUpdateDto : GroupMemberCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
