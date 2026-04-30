using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class SimpleUserMapper
{
    public static UserViewDto ToDto(User user)
    {
        return new UserViewDto
        {
            Id = user.Id,
            UserName = user.Email
        };
    }
}