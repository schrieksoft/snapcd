// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

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