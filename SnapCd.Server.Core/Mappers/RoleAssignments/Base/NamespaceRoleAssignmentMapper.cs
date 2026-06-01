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

public static class NamespaceRoleAssignmentMapper
{
    public static NamespaceRoleAssignment ToEntity(NamespaceRoleAssignmentCreateDto dto, Guid organizationId)
    {
        var id = Guid.NewGuid();

        return dto.PrincipalDiscriminator switch
        {
            RoleAssignmentPrincipalDiscriminator.User => new UserNamespaceRoleAssignment
            {
                Id = id,
                OrganizationId = organizationId,
                NamespaceId = dto.NamespaceId,
                UserId = dto.PrincipalId,
                PrincipalDiscriminator = dto.PrincipalDiscriminator,
                RoleName = dto.RoleName
            },
            RoleAssignmentPrincipalDiscriminator.ServicePrincipal => new ServicePrincipalNamespaceRoleAssignment
            {
                Id = id,
                OrganizationId = organizationId,
                NamespaceId = dto.NamespaceId,
                ServicePrincipalId = dto.PrincipalId,
                PrincipalDiscriminator = dto.PrincipalDiscriminator,
                RoleName = dto.RoleName
            },
            RoleAssignmentPrincipalDiscriminator.Group => new GroupNamespaceRoleAssignment
            {
                Id = id,
                OrganizationId = organizationId,
                NamespaceId = dto.NamespaceId,
                GroupId = dto.PrincipalId,
                PrincipalDiscriminator = dto.PrincipalDiscriminator,
                RoleName = dto.RoleName
            },
            _ => throw new ArgumentException($"Unknown PrincipalDiscriminator: {dto.PrincipalDiscriminator}")
        };
    }

    public static NamespaceRoleAssignmentReadDto ToDto(NamespaceRoleAssignment entity)
    {
        return new NamespaceRoleAssignmentReadDto
        {
            Id = entity.Id,
            NamespaceId = entity.NamespaceId,
            PrincipalId = entity.PrincipalId,
            PrincipalDiscriminator = entity.PrincipalDiscriminator,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(NamespaceRoleAssignment entity, NamespaceRoleAssignmentUpdateDto dto)
    {
        entity.NamespaceId = dto.NamespaceId;
        entity.PrincipalDiscriminator = dto.PrincipalDiscriminator;
        entity.RoleName = dto.RoleName;

        // Update type-specific properties based on discriminator
        switch (entity)
        {
            case UserNamespaceRoleAssignment userEntity:
                userEntity.UserId = dto.PrincipalId;
                break;
            case ServicePrincipalNamespaceRoleAssignment spEntity:
                spEntity.ServicePrincipalId = dto.PrincipalId;
                break;
            case GroupNamespaceRoleAssignment groupEntity:
                groupEntity.GroupId = dto.PrincipalId;
                break;
        }
    }
}