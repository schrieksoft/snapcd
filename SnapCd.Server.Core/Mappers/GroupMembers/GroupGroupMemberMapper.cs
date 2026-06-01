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

public static class GroupGroupMemberMapper
{
    public static GroupGroupMember ToEntity(GroupGroupMemberCreateDto dto, Guid organizationId)
    {
        return new GroupGroupMember
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            GroupId = dto.GroupId,
            MemberGroupId = dto.MemberGroupId
        };
    }

    public static GroupGroupMemberReadDto ToDto(GroupGroupMember entity)
    {
        return new GroupGroupMemberReadDto
        {
            Id = entity.Id,
            GroupId = entity.GroupId,
            MemberGroupId = entity.MemberGroupId
        };
    }

    public static void UpdateEntity(GroupGroupMember entity, GroupGroupMemberUpdateDto dto)
    {
        entity.GroupId = dto.GroupId;
        entity.MemberGroupId = dto.MemberGroupId;
    }
}