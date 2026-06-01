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

public static class ServicePrincipalRunnerRoleAssignmentMapper
{
    public static ServicePrincipalRunnerRoleAssignment ToEntity(ServicePrincipalRunnerRoleAssignmentCreateDto dto, Guid organizationId)
    {
        return new ServicePrincipalRunnerRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ServicePrincipalId = dto.ServicePrincipalId,
            RunnerId = dto.RunnerId,
            RoleName = dto.RoleName
        };
    }

    public static ServicePrincipalRunnerRoleAssignmentReadDto ToDto(ServicePrincipalRunnerRoleAssignment entity)
    {
        return new ServicePrincipalRunnerRoleAssignmentReadDto
        {
            Id = entity.Id,
            ServicePrincipalId = entity.ServicePrincipalId,
            RunnerId = entity.RunnerId,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(ServicePrincipalRunnerRoleAssignment entity, ServicePrincipalRunnerRoleAssignmentUpdateDto dto)
    {
        entity.ServicePrincipalId = dto.ServicePrincipalId;
        entity.RunnerId = dto.RunnerId;
        entity.RoleName = dto.RoleName;
    }
}