using SnapCd.Server.Core.Dtos.Users;
using SnapCd.Server.Core.Entities.Definition;

namespace SnapCd.Server.Core.Mappers;

public static class UserMapper
{
    public static User ToEntity(UserCreateDto dto)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            IsDisabled = dto.IsDisabled,
            UserName = dto.UserName,
            NormalizedUserName = dto.NormalizedUserName,
            Email = dto.Email,
            NormalizedEmail = dto.NormalizedEmail,
            EmailConfirmed = dto.EmailConfirmed,
            PhoneNumber = dto.PhoneNumber,
            PhoneNumberConfirmed = dto.PhoneNumberConfirmed,
            TwoFactorEnabled = dto.TwoFactorEnabled,
            LockoutEnd = dto.LockoutEnd,
            LockoutEnabled = dto.LockoutEnabled,
            AccessFailedCount = dto.AccessFailedCount
        };
    }

    public static UserReadDto ToDto(User entity)
    {
        return new UserReadDto
        {
            Id = entity.Id,
            IsDisabled = entity.IsDisabled,
            UserName = entity.UserName,
            NormalizedUserName = entity.NormalizedUserName,
            Email = entity.Email,
            NormalizedEmail = entity.NormalizedEmail,
            EmailConfirmed = entity.EmailConfirmed,
            PhoneNumber = entity.PhoneNumber,
            PhoneNumberConfirmed = entity.PhoneNumberConfirmed,
            TwoFactorEnabled = entity.TwoFactorEnabled,
            LockoutEnd = entity.LockoutEnd,
            LockoutEnabled = entity.LockoutEnabled,
            AccessFailedCount = entity.AccessFailedCount
        };
    }

    public static void UpdateEntity(User entity, UserUpdateDto dto)
    {
        entity.IsDisabled = dto.IsDisabled;
        entity.UserName = dto.UserName;
        entity.NormalizedUserName = dto.NormalizedUserName;
        entity.Email = dto.Email;
        entity.NormalizedEmail = dto.NormalizedEmail;
        entity.EmailConfirmed = dto.EmailConfirmed;
        entity.PhoneNumber = dto.PhoneNumber;
        entity.PhoneNumberConfirmed = dto.PhoneNumberConfirmed;
        entity.TwoFactorEnabled = dto.TwoFactorEnabled;
        entity.LockoutEnd = dto.LockoutEnd;
        entity.LockoutEnabled = dto.LockoutEnabled;
        entity.AccessFailedCount = dto.AccessFailedCount;
    }
}