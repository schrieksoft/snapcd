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

public static class UserRunnerRoleAssignmentMapper
{
    public static UserRunnerRoleAssignment ToEntity(UserRunnerRoleAssignmentCreateDto dto, Guid organizationId)
    {
        return new UserRunnerRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = dto.UserId,
            RunnerId = dto.RunnerId,
            RoleName = dto.RoleName
        };
    }

    public static UserRunnerRoleAssignmentReadDto ToDto(UserRunnerRoleAssignment entity)
    {
        return new UserRunnerRoleAssignmentReadDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            RunnerId = entity.RunnerId,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(UserRunnerRoleAssignment entity, UserRunnerRoleAssignmentUpdateDto dto)
    {
        entity.UserId = dto.UserId;
        entity.RunnerId = dto.RunnerId;
        entity.RoleName = dto.RoleName;
    }
}