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
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Agent.Base;

namespace SnapCd.Server.Core.Mappers.RoleAssignments.Base;

public static class AgentRoleAssignmentMapper
{
    public static AgentRoleAssignment ToEntity(AgentRoleAssignmentCreateDto dto, Guid organizationId)
    {
        var id = Guid.NewGuid();

        return dto.PrincipalDiscriminator switch
        {
            RoleAssignmentPrincipalDiscriminator.User => new UserAgentRoleAssignment
            {
                Id = id,
                OrganizationId = organizationId,
                AgentId = dto.AgentId,
                UserId = dto.PrincipalId,
                PrincipalDiscriminator = dto.PrincipalDiscriminator,
                RoleName = dto.RoleName
            },
            RoleAssignmentPrincipalDiscriminator.ServicePrincipal => new ServicePrincipalAgentRoleAssignment
            {
                Id = id,
                OrganizationId = organizationId,
                AgentId = dto.AgentId,
                ServicePrincipalId = dto.PrincipalId,
                PrincipalDiscriminator = dto.PrincipalDiscriminator,
                RoleName = dto.RoleName
            },
            RoleAssignmentPrincipalDiscriminator.Group => new GroupAgentRoleAssignment
            {
                Id = id,
                OrganizationId = organizationId,
                AgentId = dto.AgentId,
                GroupId = dto.PrincipalId,
                PrincipalDiscriminator = dto.PrincipalDiscriminator,
                RoleName = dto.RoleName
            },
            _ => throw new ArgumentException($"Unknown PrincipalDiscriminator: {dto.PrincipalDiscriminator}")
        };
    }

    public static AgentRoleAssignmentReadDto ToDto(AgentRoleAssignment entity)
    {
        return new AgentRoleAssignmentReadDto
        {
            Id = entity.Id,
            AgentId = entity.AgentId,
            PrincipalId = entity.PrincipalId,
            PrincipalDiscriminator = entity.PrincipalDiscriminator,
            RoleName = entity.RoleName
        };
    }

    public static void UpdateEntity(AgentRoleAssignment entity, AgentRoleAssignmentUpdateDto dto)
    {
        entity.AgentId = dto.AgentId;
        entity.PrincipalDiscriminator = dto.PrincipalDiscriminator;
        entity.RoleName = dto.RoleName;

        switch (entity)
        {
            case UserAgentRoleAssignment userEntity:
                userEntity.UserId = dto.PrincipalId;
                break;
            case ServicePrincipalAgentRoleAssignment spEntity:
                spEntity.ServicePrincipalId = dto.PrincipalId;
                break;
            case GroupAgentRoleAssignment groupEntity:
                groupEntity.GroupId = dto.PrincipalId;
                break;
        }
    }
}
