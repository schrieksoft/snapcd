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

public static class ServicePrincipalModuleRoleAssignmentMapper
{
    public static ServicePrincipalModuleRoleAssignment ToEntity(ServicePrincipalModuleRoleAssignmentCreateDto dto, Guid organizationId)
    {
        return new ServicePrincipalModuleRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            ServicePrincipalId = dto.ServicePrincipalId,
            ModuleId = dto.ModuleId,
            RoleName = dto.RoleName
        };
    }

    public static ServicePrincipalModuleRoleAssignmentReadDto ToDto(ServicePrincipalModuleRoleAssignment entity)
    {
        return new ServicePrincipalModuleRoleAssignmentReadDto
        {
            Id = entity.Id,
            ServicePrincipalId = entity.ServicePrincipalId,
            ModuleId = entity.ModuleId,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(ServicePrincipalModuleRoleAssignment entity, ServicePrincipalModuleRoleAssignmentUpdateDto dto)
    {
        entity.ServicePrincipalId = dto.ServicePrincipalId;
        entity.ModuleId = dto.ModuleId;
        entity.RoleName = dto.RoleName;
    }
}