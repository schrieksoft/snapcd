using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.GroupMembers;

public class GroupGroupMemberReadDto : GroupGroupMemberCreateDto, IDto
{
    public Guid Id { get; set; }
}