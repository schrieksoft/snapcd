using SnapCd.Contracts.Interfaces;

namespace SnapCd.Server.Core.Dtos.Users;

public class UserReadDto : UserCreateDto, IDto
{
    public Guid Id { get; set; }
}