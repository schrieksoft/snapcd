using SnapCd.Contracts.Interfaces;

namespace SnapCd.Server.Core.Dtos.UserInvitations;

public class UserInvitationReadDto : UserInvitationCreateDto, IDto
{
    public Guid Id { get; set; }
}