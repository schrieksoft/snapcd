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
using SnapCd.Server.Core.Mappers.RoleAssignments.Base;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments.Base;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Services.Crud.RoleAssignment;

public class AgentRoleAssignmentServiceFactory(
    AgentRoleAssignmentSecuredRepositoryFactory agentRoleAssignmentSecuredRepositoryFactory,
    UserAgentRoleAssignmentSecuredRepositoryFactory userAgentSecuredRepositoryFactory,
    ServicePrincipalAgentRoleAssignmentSecuredRepositoryFactory servicePrincipalAgentSecuredRepositoryFactory,
    GroupAgentRoleAssignmentSecuredRepositoryFactory groupAgentSecuredRepositoryFactory)
{
    public AgentRoleAssignmentService Create(IPrincipalProvider? principalProvider = null)
    {
        var baseRepo = agentRoleAssignmentSecuredRepositoryFactory.Create(principalProvider);
        var userRepo = userAgentSecuredRepositoryFactory.Create(principalProvider);
        var servicePrincipalRepo = servicePrincipalAgentSecuredRepositoryFactory.Create(principalProvider);
        var groupRepo = groupAgentSecuredRepositoryFactory.Create(principalProvider);

        return new AgentRoleAssignmentService(
            baseRepo,
            userRepo,
            servicePrincipalRepo,
            groupRepo);
    }
}

public class AgentRoleAssignmentService : IDisposable
{
    protected readonly AgentRoleAssignmentSecuredRepository BaseSecuredRepository;
    protected readonly UserAgentRoleAssignmentSecuredRepository UserSecuredRepository;
    protected readonly ServicePrincipalAgentRoleAssignmentSecuredRepository ServicePrincipalSecuredRepository;
    protected readonly GroupAgentRoleAssignmentSecuredRepository GroupSecuredRepository;

    public AgentRoleAssignmentService(
        AgentRoleAssignmentSecuredRepository baseSecuredRepository,
        UserAgentRoleAssignmentSecuredRepository userSecuredRepository,
        ServicePrincipalAgentRoleAssignmentSecuredRepository servicePrincipalSecuredRepository,
        GroupAgentRoleAssignmentSecuredRepository groupSecuredRepository)
    {
        BaseSecuredRepository = baseSecuredRepository;
        UserSecuredRepository = userSecuredRepository;
        ServicePrincipalSecuredRepository = servicePrincipalSecuredRepository;
        GroupSecuredRepository = groupSecuredRepository;
    }

    protected virtual AgentRoleAssignment MapToEntity(AgentRoleAssignmentReadDto dto, Guid organizationId)
    {
        return AgentRoleAssignmentMapper.ToEntity(dto, organizationId);
    }

    protected virtual AgentRoleAssignmentReadDto MapToDto(AgentRoleAssignment entity)
    {
        return AgentRoleAssignmentMapper.ToDto(entity);
    }

    protected virtual void UpdateEntityFromDto(AgentRoleAssignment entity, AgentRoleAssignmentUpdateDto dto)
    {
        AgentRoleAssignmentMapper.UpdateEntity(entity, dto);
    }

    public virtual void Dispose()
    {
        BaseSecuredRepository.Dispose();
        UserSecuredRepository.Dispose();
        ServicePrincipalSecuredRepository.Dispose();
        GroupSecuredRepository.Dispose();
    }

    public virtual async Task<AgentRoleAssignmentReadDto> Get(Guid id, Guid organizationId)
    {
        var entity = await BaseSecuredRepository.Get(id, organizationId);
        return MapToDto(entity);
    }

    public virtual async Task<List<AgentRoleAssignmentReadDto>> List(Guid organizationId)
    {
        var entities = await BaseSecuredRepository.List(organizationId);
        return entities.Select(MapToDto).ToList();
    }

    public virtual async Task<AgentRoleAssignmentReadDto> Create(AgentRoleAssignmentReadDto dto, Guid organizationId)
    {
        var entity = MapToEntity(dto, organizationId);

        entity = dto.PrincipalDiscriminator switch
        {
            RoleAssignmentPrincipalDiscriminator.User => await UserSecuredRepository.Create((UserAgentRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.ServicePrincipal => await ServicePrincipalSecuredRepository.Create((ServicePrincipalAgentRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.Group => await GroupSecuredRepository.Create((GroupAgentRoleAssignment)entity),
            _ => throw new ArgumentException($"Unknown PrincipalDiscriminator: {dto.PrincipalDiscriminator}")
        };

        return MapToDto(entity);
    }

    public virtual async Task<AgentRoleAssignmentReadDto> Update(AgentRoleAssignmentUpdateDto dto, Guid id, Guid organizationId)
    {
        var entity = await BaseSecuredRepository.Get(id, organizationId);

        if (entity.PrincipalDiscriminator != dto.PrincipalDiscriminator)
            throw new InvalidOperationException(
                $"Cannot change PrincipalDiscriminator from {entity.PrincipalDiscriminator} to {dto.PrincipalDiscriminator}. " +
                "Delete the existing role assignment and create a new one instead.");

        UpdateEntityFromDto(entity, dto);

        entity = entity.PrincipalDiscriminator switch
        {
            RoleAssignmentPrincipalDiscriminator.User => await UserSecuredRepository.Update((UserAgentRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.ServicePrincipal => await ServicePrincipalSecuredRepository.Update((ServicePrincipalAgentRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.Group => await GroupSecuredRepository.Update((GroupAgentRoleAssignment)entity),
            _ => throw new ArgumentException($"Unknown PrincipalDiscriminator: {entity.PrincipalDiscriminator}")
        };

        return MapToDto(entity);
    }

    public virtual async Task Delete(Guid id, Guid organizationId)
    {
        await BaseSecuredRepository.Delete(id, organizationId);
    }
}
