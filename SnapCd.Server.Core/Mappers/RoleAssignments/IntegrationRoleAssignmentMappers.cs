// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts;
using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Integration.Base;

namespace SnapCd.Server.Core.Mappers.RoleAssignments;

public static class IntegrationRoleAssignmentMapper
{
    public static IntegrationRoleAssignment ToEntity(IntegrationRoleAssignmentCreateDto dto, Guid organizationId)
    {
        var id = Guid.NewGuid();
        return dto.PrincipalDiscriminator switch
        {
            RoleAssignmentPrincipalDiscriminator.User => new UserIntegrationRoleAssignment
            {
                Id = id, OrganizationId = organizationId, IntegrationId = dto.IntegrationId,
                UserId = dto.PrincipalId, PrincipalDiscriminator = dto.PrincipalDiscriminator, RoleName = dto.RoleName
            },
            RoleAssignmentPrincipalDiscriminator.ServicePrincipal => new ServicePrincipalIntegrationRoleAssignment
            {
                Id = id, OrganizationId = organizationId, IntegrationId = dto.IntegrationId,
                ServicePrincipalId = dto.PrincipalId, PrincipalDiscriminator = dto.PrincipalDiscriminator, RoleName = dto.RoleName
            },
            RoleAssignmentPrincipalDiscriminator.Group => new GroupIntegrationRoleAssignment
            {
                Id = id, OrganizationId = organizationId, IntegrationId = dto.IntegrationId,
                GroupId = dto.PrincipalId, PrincipalDiscriminator = dto.PrincipalDiscriminator, RoleName = dto.RoleName
            },
            _ => throw new ArgumentException($"Unknown PrincipalDiscriminator: {dto.PrincipalDiscriminator}")
        };
    }

    public static IntegrationRoleAssignmentReadDto ToDto(IntegrationRoleAssignment entity)
        => new()
        {
            Id = entity.Id,
            IntegrationId = entity.IntegrationId,
            PrincipalId = entity.PrincipalId,
            PrincipalDiscriminator = entity.PrincipalDiscriminator,
            RoleName = entity.RoleName
        };

    public static void UpdateEntity(IntegrationRoleAssignment entity, IntegrationRoleAssignmentUpdateDto dto)
    {
        entity.IntegrationId = dto.IntegrationId;
        entity.PrincipalDiscriminator = dto.PrincipalDiscriminator;
        entity.RoleName = dto.RoleName;
        switch (entity)
        {
            case UserIntegrationRoleAssignment u: u.UserId = dto.PrincipalId; break;
            case ServicePrincipalIntegrationRoleAssignment sp: sp.ServicePrincipalId = dto.PrincipalId; break;
            case GroupIntegrationRoleAssignment g: g.GroupId = dto.PrincipalId; break;
        }
    }
}

public static class UserIntegrationRoleAssignmentMapper
{
    public static UserIntegrationRoleAssignmentReadDto ToDto(UserIntegrationRoleAssignment e)
        => new() { Id = e.Id, UserId = e.UserId, IntegrationId = e.IntegrationId, RoleName = e.RoleName };
}

public static class ServicePrincipalIntegrationRoleAssignmentMapper
{
    public static ServicePrincipalIntegrationRoleAssignmentReadDto ToDto(ServicePrincipalIntegrationRoleAssignment e)
        => new() { Id = e.Id, ServicePrincipalId = e.ServicePrincipalId, IntegrationId = e.IntegrationId, RoleName = e.RoleName };
}

public static class GroupIntegrationRoleAssignmentMapper
{
    public static GroupIntegrationRoleAssignmentReadDto ToDto(GroupIntegrationRoleAssignment e)
        => new() { Id = e.Id, GroupId = e.GroupId, IntegrationId = e.IntegrationId, RoleName = e.RoleName };
}
