using SnapCd.Contracts.Interfaces;

namespace SnapCd.Server.Core.Dtos.UserInvitations;

public class UserInvitationUpdateDto : UserInvitationCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
