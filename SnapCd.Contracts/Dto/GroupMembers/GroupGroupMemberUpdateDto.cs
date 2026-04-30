using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.GroupMembers;

public class GroupGroupMemberUpdateDto : GroupGroupMemberCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
