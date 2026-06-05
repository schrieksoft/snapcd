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

public static class UserAgentRoleAssignmentMapper
{
    public static UserAgentRoleAssignment ToEntity(UserAgentRoleAssignmentCreateDto dto, Guid organizationId)
    {
        return new UserAgentRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = dto.UserId,
            AgentId = dto.AgentId,
            RoleName = dto.RoleName
        };
    }

    public static UserAgentRoleAssignmentReadDto ToDto(UserAgentRoleAssignment entity)
    {
        return new UserAgentRoleAssignmentReadDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            AgentId = entity.AgentId,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(UserAgentRoleAssignment entity, UserAgentRoleAssignmentUpdateDto dto)
    {
        entity.UserId = dto.UserId;
        entity.AgentId = dto.AgentId;
        entity.RoleName = dto.RoleName;
    }
}
