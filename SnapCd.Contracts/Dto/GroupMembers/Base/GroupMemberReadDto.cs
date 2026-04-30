using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.GroupMembers.Base;

public class GroupMemberReadDto : GroupMemberCreateDto, IDto
{
    public Guid Id { get; set; }
}