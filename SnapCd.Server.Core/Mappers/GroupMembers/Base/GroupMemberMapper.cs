// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Contracts.Dto.GroupMembers.Base;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;

namespace SnapCd.Server.Core.Mappers.GroupMembers.Base;

public static class GroupMemberMapper
{
    public static GroupMember ToEntity(GroupMemberCreateDto dto, Guid organizationId)
    {
        var id = Guid.NewGuid();

        return dto.GroupMemberDiscriminator switch
        {
            GroupMemberDiscriminator.User => new UserGroupMember
            {
                Id = id,
                OrganizationId = organizationId,
                GroupId = dto.GroupId,
                UserId = dto.PrincipalId,
                GroupMemberDiscriminator = dto.GroupMemberDiscriminator
            },
            GroupMemberDiscriminator.ServicePrincipal => new ServicePrincipalGroupMember
            {
                Id = id,
                OrganizationId = organizationId,
                GroupId = dto.GroupId,
                ServicePrincipalId = dto.PrincipalId,
                GroupMemberDiscriminator = dto.GroupMemberDiscriminator
            },
            GroupMemberDiscriminator.Group => new GroupGroupMember
            {
                Id = id,
                OrganizationId = organizationId,
                GroupId = dto.GroupId,
                MemberGroupId = dto.PrincipalId,
                GroupMemberDiscriminator = dto.GroupMemberDiscriminator
            },
            _ => throw new ArgumentException($"Unknown GroupMemberDiscriminator: {dto.GroupMemberDiscriminator}")
        };
    }

    public static GroupMemberReadDto ToDto(GroupMember entity)
    {
        return new GroupMemberReadDto
        {
            Id = entity.Id,
            GroupId = entity.GroupId,
            PrincipalId = entity.PrincipalId,
            GroupMemberDiscriminator = entity.GroupMemberDiscriminator
        };
    }

    public static void UpdateEntity(GroupMember entity, GroupMemberUpdateDto dto)
    {
        entity.GroupId = dto.GroupId;
        entity.GroupMemberDiscriminator = dto.GroupMemberDiscriminator;

        // Update type-specific properties based on discriminator
        switch (entity)
        {
            case UserGroupMember userMember:
                userMember.UserId = dto.PrincipalId;
                break;
            case ServicePrincipalGroupMember spMember:
                spMember.ServicePrincipalId = dto.PrincipalId;
                break;
            case GroupGroupMember groupMember:
                groupMember.MemberGroupId = dto.PrincipalId;
                break;
        }
    }
}