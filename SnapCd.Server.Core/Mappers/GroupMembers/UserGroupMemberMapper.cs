// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.GroupMembers;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;

namespace SnapCd.Server.Core.Mappers.GroupMembers;

public static class UserGroupMemberMapper
{
    public static UserGroupMember ToEntity(UserGroupMemberCreateDto dto, Guid organizationId)
    {
        return new UserGroupMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            GroupId = dto.GroupId,
            UserId = dto.UserId
        };
    }

    public static UserGroupMemberReadDto ToDto(UserGroupMember entity)
    {
        return new UserGroupMemberReadDto
        {
            Id = entity.Id,
            GroupId = entity.GroupId,
            UserId = entity.UserId
        };
    }

    public static void UpdateEntity(UserGroupMember entity, UserGroupMemberUpdateDto dto)
    {
        entity.GroupId = dto.GroupId;
        entity.UserId = dto.UserId;
    }
}