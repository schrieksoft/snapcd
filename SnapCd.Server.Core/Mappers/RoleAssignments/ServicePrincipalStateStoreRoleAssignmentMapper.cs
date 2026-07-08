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

public static class ServicePrincipalStateStoreRoleAssignmentMapper
{
    public static ServicePrincipalStateStoreRoleAssignment ToEntity(ServicePrincipalStateStoreRoleAssignmentCreateDto dto, Guid organizationId)
    {
        return new ServicePrincipalStateStoreRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ServicePrincipalId = dto.ServicePrincipalId,
            StateStoreId = dto.StateStoreId,
            RoleName = dto.RoleName
        };
    }

    public static ServicePrincipalStateStoreRoleAssignmentReadDto ToDto(ServicePrincipalStateStoreRoleAssignment entity)
    {
        return new ServicePrincipalStateStoreRoleAssignmentReadDto
        {
            Id = entity.Id,
            ServicePrincipalId = entity.ServicePrincipalId,
            StateStoreId = entity.StateStoreId,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(ServicePrincipalStateStoreRoleAssignment entity, ServicePrincipalStateStoreRoleAssignmentUpdateDto dto)
    {
        entity.ServicePrincipalId = dto.ServicePrincipalId;
        entity.StateStoreId = dto.StateStoreId;
        entity.RoleName = dto.RoleName;
    }
}
