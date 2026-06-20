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
using SnapCd.Server.Core.Mappers.RoleAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments;

namespace SnapCd.Server.Core.Services.Crud.RoleAssignment;

/// <summary>
/// Manages integration role assignments. Get/List/Delete go through the base (org-IAM gated) secured repo;
/// Create/Update dispatch to the per-principal secured repos (org-IAM + per-integration role gated). Mirrors
/// <c>AgentRoleAssignmentService</c>.
/// </summary>
public class IntegrationRoleAssignmentService(
    IntegrationRoleAssignmentSecuredRepository baseSecuredRepository,
    UserIntegrationRoleAssignmentSecuredRepository userSecuredRepository,
    ServicePrincipalIntegrationRoleAssignmentSecuredRepository servicePrincipalSecuredRepository,
    GroupIntegrationRoleAssignmentSecuredRepository groupSecuredRepository) : IDisposable
{
    public void Dispose()
    {
        baseSecuredRepository.Dispose();
        userSecuredRepository.Dispose();
        servicePrincipalSecuredRepository.Dispose();
        groupSecuredRepository.Dispose();
    }

    public async Task<IntegrationRoleAssignmentReadDto> Get(Guid id, Guid organizationId)
        => IntegrationRoleAssignmentMapper.ToDto(await baseSecuredRepository.Get(id, organizationId));

    public async Task<List<IntegrationRoleAssignmentReadDto>> List(Guid organizationId)
        => (await baseSecuredRepository.List(organizationId)).Select(IntegrationRoleAssignmentMapper.ToDto).ToList();

    public async Task<IntegrationRoleAssignmentReadDto> Create(IntegrationRoleAssignmentReadDto dto, Guid organizationId)
    {
        var entity = IntegrationRoleAssignmentMapper.ToEntity(dto, organizationId);

        entity = dto.PrincipalDiscriminator switch
        {
            RoleAssignmentPrincipalDiscriminator.User => await userSecuredRepository.Create((UserIntegrationRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.ServicePrincipal => await servicePrincipalSecuredRepository.Create((ServicePrincipalIntegrationRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.Group => await groupSecuredRepository.Create((GroupIntegrationRoleAssignment)entity),
            _ => throw new ArgumentException($"Unknown PrincipalDiscriminator: {dto.PrincipalDiscriminator}")
        };

        return IntegrationRoleAssignmentMapper.ToDto(entity);
    }

    public async Task<IntegrationRoleAssignmentReadDto> Update(IntegrationRoleAssignmentUpdateDto dto, Guid id, Guid organizationId)
    {
        var entity = await baseSecuredRepository.Get(id, organizationId);

        if (entity.PrincipalDiscriminator != dto.PrincipalDiscriminator)
            throw new InvalidOperationException(
                $"Cannot change PrincipalDiscriminator from {entity.PrincipalDiscriminator} to {dto.PrincipalDiscriminator}. " +
                "Delete the existing role assignment and create a new one instead.");

        IntegrationRoleAssignmentMapper.UpdateEntity(entity, dto);

        entity = entity.PrincipalDiscriminator switch
        {
            RoleAssignmentPrincipalDiscriminator.User => await userSecuredRepository.Update((UserIntegrationRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.ServicePrincipal => await servicePrincipalSecuredRepository.Update((ServicePrincipalIntegrationRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.Group => await groupSecuredRepository.Update((GroupIntegrationRoleAssignment)entity),
            _ => throw new ArgumentException($"Unknown PrincipalDiscriminator: {entity.PrincipalDiscriminator}")
        };

        return IntegrationRoleAssignmentMapper.ToDto(entity);
    }

    public async Task Delete(Guid id, Guid organizationId)
        => await baseSecuredRepository.Delete(id, organizationId);
}
