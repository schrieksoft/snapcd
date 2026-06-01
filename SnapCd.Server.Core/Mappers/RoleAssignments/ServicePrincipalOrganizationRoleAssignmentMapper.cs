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

public static class ServicePrincipalOrganizationRoleAssignmentMapper
{
    public static ServicePrincipalOrganizationRoleAssignment ToEntity(ServicePrincipalOrganizationRoleAssignmentCreateDto dto, Guid organizationId)
    {
        return new ServicePrincipalOrganizationRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ServicePrincipalId = dto.ServicePrincipalId,
            RoleName = dto.RoleName
        };
    }

    public static ServicePrincipalOrganizationRoleAssignmentReadDto ToDto(ServicePrincipalOrganizationRoleAssignment entity)
    {
        return new ServicePrincipalOrganizationRoleAssignmentReadDto
        {
            Id = entity.Id,
            ServicePrincipalId = entity.ServicePrincipalId,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(ServicePrincipalOrganizationRoleAssignment entity, ServicePrincipalOrganizationRoleAssignmentUpdateDto dto)
    {
        entity.ServicePrincipalId = dto.ServicePrincipalId;
        entity.RoleName = dto.RoleName;
    }
}