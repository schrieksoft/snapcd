using SnapCd.Contracts.Interfaces;

namespace SnapCd.Contracts.Dto.GroupMembers;

public class UserGroupMemberReadDto : UserGroupMemberCreateDto, IDto
{
    public Guid Id { get; set; }
}