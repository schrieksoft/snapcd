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

public static class UserOrganizationRoleAssignmentMapper
{
    public static UserOrganizationRoleAssignment ToEntity(UserOrganizationRoleAssignmentCreateDto dto, Guid organizationId)
    {
        return new UserOrganizationRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = dto.UserId,
            RoleName = dto.RoleName
        };
    }

    public static UserOrganizationRoleAssignmentReadDto ToDto(UserOrganizationRoleAssignment entity)
    {
        return new UserOrganizationRoleAssignmentReadDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(UserOrganizationRoleAssignment entity, UserOrganizationRoleAssignmentUpdateDto dto)
    {
        entity.UserId = dto.UserId;
        entity.RoleName = dto.RoleName;
    }
}