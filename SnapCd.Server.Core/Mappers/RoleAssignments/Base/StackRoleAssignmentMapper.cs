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

public static class StackRoleAssignmentMapper
{
    public static StackRoleAssignment ToEntity(StackRoleAssignmentCreateDto dto, Guid organizationId)
    {
        var id = Guid.NewGuid();

        return dto.PrincipalDiscriminator switch
        {
            RoleAssignmentPrincipalDiscriminator.User => new UserStackRoleAssignment
            {
                Id = id,
                OrganizationId = organizationId,
                StackId = dto.StackId,
                UserId = dto.PrincipalId,
                PrincipalDiscriminator = dto.PrincipalDiscriminator,
                RoleName = dto.RoleName
            },
            RoleAssignmentPrincipalDiscriminator.ServicePrincipal => new ServicePrincipalStackRoleAssignment
            {
                Id = id,
                OrganizationId = organizationId,
                StackId = dto.StackId,
                ServicePrincipalId = dto.PrincipalId,
                PrincipalDiscriminator = dto.PrincipalDiscriminator,
                RoleName = dto.RoleName
            },
            RoleAssignmentPrincipalDiscriminator.Group => new GroupStackRoleAssignment
            {
                Id = id,
                OrganizationId = organizationId,
                StackId = dto.StackId,
                GroupId = dto.PrincipalId,
                PrincipalDiscriminator = dto.PrincipalDiscriminator,
                RoleName = dto.RoleName
            },
            _ => throw new ArgumentException($"Unknown PrincipalDiscriminator: {dto.PrincipalDiscriminator}")
        };
    }

    public static StackRoleAssignmentDto ToDto(StackRoleAssignment entity)
    {
        return new StackRoleAssignmentDto
        {
            Id = entity.Id,
            StackId = entity.StackId,
            PrincipalId = entity.PrincipalId,
            PrincipalDiscriminator = entity.PrincipalDiscriminator,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(StackRoleAssignment entity, StackRoleAssignmentUpdateDto dto)
    {
        entity.StackId = dto.StackId;
        entity.PrincipalDiscriminator = dto.PrincipalDiscriminator;
        entity.RoleName = dto.RoleName;

        // Update type-specific properties based on discriminator
        switch (entity)
        {
            case UserStackRoleAssignment userEntity:
                userEntity.UserId = dto.PrincipalId;
                break;
            case ServicePrincipalStackRoleAssignment spEntity:
                spEntity.ServicePrincipalId = dto.PrincipalId;
                break;
            case GroupStackRoleAssignment groupEntity:
                groupEntity.GroupId = dto.PrincipalId;
                break;
        }
    }
}