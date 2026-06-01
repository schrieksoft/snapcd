// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Contracts.Dto.RoleAssignments.Base;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Base;

namespace SnapCd.Server.Core.Mappers.RoleAssignments.Base;

public static class ModuleRoleAssignmentMapper
{
    public static ModuleRoleAssignment ToEntity(ModuleRoleAssignmentCreateDto dto, Guid organizationId)
    {
        var id = Guid.NewGuid();

        return dto.PrincipalDiscriminator switch
        {
            RoleAssignmentPrincipalDiscriminator.User => new UserModuleRoleAssignment
            {
                Id = id,
                OrganizationId = organizationId,
                ModuleId = dto.ModuleId,
                UserId = dto.PrincipalId,
                PrincipalDiscriminator = dto.PrincipalDiscriminator,
                RoleName = dto.RoleName
            },
            RoleAssignmentPrincipalDiscriminator.ServicePrincipal => new ServicePrincipalModuleRoleAssignment
            {
                Id = id,
                OrganizationId = organizationId,
                ModuleId = dto.ModuleId,
                ServicePrincipalId = dto.PrincipalId,
                PrincipalDiscriminator = dto.PrincipalDiscriminator,
                RoleName = dto.RoleName
            },
            RoleAssignmentPrincipalDiscriminator.Group => new GroupModuleRoleAssignment
            {
                Id = id,
                OrganizationId = organizationId,
                ModuleId = dto.ModuleId,
                GroupId = dto.PrincipalId,
                PrincipalDiscriminator = dto.PrincipalDiscriminator,
                RoleName = dto.RoleName
            },
            _ => throw new ArgumentException($"Unknown PrincipalDiscriminator: {dto.PrincipalDiscriminator}")
        };
    }

    public static ModuleRoleAssignmentReadDto ToDto(ModuleRoleAssignment entity)
    {
        return new ModuleRoleAssignmentReadDto
        {
            Id = entity.Id,
            ModuleId = entity.ModuleId,
            PrincipalId = entity.PrincipalId,
            PrincipalDiscriminator = entity.PrincipalDiscriminator,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(ModuleRoleAssignment entity, ModuleRoleAssignmentUpdateDto dto)
    {
        entity.ModuleId = dto.ModuleId;
        entity.PrincipalDiscriminator = dto.PrincipalDiscriminator;
        entity.RoleName = dto.RoleName;

        // Update type-specific properties based on discriminator
        switch (entity)
        {
            case UserModuleRoleAssignment userEntity:
                userEntity.UserId = dto.PrincipalId;
                break;
            case ServicePrincipalModuleRoleAssignment spEntity:
                spEntity.ServicePrincipalId = dto.PrincipalId;
                break;
            case GroupModuleRoleAssignment groupEntity:
                groupEntity.GroupId = dto.PrincipalId;
                break;
        }
    }
}