using SnapCd.Contracts.Interfaces;

namespace SnapCd.Server.Core.Dtos.Users;

public class UserUpdateDto : UserCreateDto, IUpdateDto
{
    public Guid Id { get; set; }
}
