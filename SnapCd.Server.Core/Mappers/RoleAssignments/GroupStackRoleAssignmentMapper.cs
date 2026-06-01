// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;

namespace SnapCd.Server.Core.Mappers.RoleAssignments;

public static class GroupStackRoleAssignmentMapper
{
    public static GroupStackRoleAssignment ToEntity(GroupStackRoleAssignmentCreateDto dto, Guid organizationId)
    {
        return new GroupStackRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            GroupId = dto.GroupId,
            StackId = dto.StackId,
            RoleName = dto.RoleName
        };
    }

    public static GroupStackRoleAssignmentReadDto ToDto(GroupStackRoleAssignment entity)
    {
        return new GroupStackRoleAssignmentReadDto
        {
            Id = entity.Id,
            GroupId = entity.GroupId,
            StackId = entity.StackId,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(GroupStackRoleAssignment entity, GroupStackRoleAssignmentUpdateDto dto)
    {
        entity.GroupId = dto.GroupId;
        entity.StackId = dto.StackId;
        entity.RoleName = dto.RoleName;
    }
}