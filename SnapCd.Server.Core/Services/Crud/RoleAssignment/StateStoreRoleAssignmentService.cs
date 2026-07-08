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
using SnapCd.Server.Core.Mappers.RoleAssignments.Base;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments.Base;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Services.Crud.RoleAssignment;

public class StateStoreRoleAssignmentServiceFactory(
    StateStoreRoleAssignmentSecuredRepositoryFactory stateStoreRoleAssignmentSecuredRepositoryFactory,
    UserStateStoreRoleAssignmentSecuredRepositoryFactory userStateStoreSecuredRepositoryFactory,
    ServicePrincipalStateStoreRoleAssignmentSecuredRepositoryFactory servicePrincipalStateStoreSecuredRepositoryFactory,
    GroupStateStoreRoleAssignmentSecuredRepositoryFactory groupStateStoreSecuredRepositoryFactory)
{
    public StateStoreRoleAssignmentService Create(IPrincipalProvider? principalProvider = null)
    {
        var baseRepo = stateStoreRoleAssignmentSecuredRepositoryFactory.Create(principalProvider);
        var userRepo = userStateStoreSecuredRepositoryFactory.Create(principalProvider);
        var servicePrincipalRepo = servicePrincipalStateStoreSecuredRepositoryFactory.Create(principalProvider);
        var groupRepo = groupStateStoreSecuredRepositoryFactory.Create(principalProvider);

        return new StateStoreRoleAssignmentService(
            baseRepo,
            userRepo,
            servicePrincipalRepo,
            groupRepo);
    }
}

public class StateStoreRoleAssignmentService : IDisposable
{
    protected readonly StateStoreRoleAssignmentSecuredRepository BaseSecuredRepository;
    protected readonly UserStateStoreRoleAssignmentSecuredRepository UserSecuredRepository;
    protected readonly ServicePrincipalStateStoreRoleAssignmentSecuredRepository ServicePrincipalSecuredRepository;
    protected readonly GroupStateStoreRoleAssignmentSecuredRepository GroupSecuredRepository;

    public StateStoreRoleAssignmentService(
        StateStoreRoleAssignmentSecuredRepository baseSecuredRepository,
        UserStateStoreRoleAssignmentSecuredRepository userSecuredRepository,
        ServicePrincipalStateStoreRoleAssignmentSecuredRepository servicePrincipalSecuredRepository,
        GroupStateStoreRoleAssignmentSecuredRepository groupSecuredRepository)
    {
        BaseSecuredRepository = baseSecuredRepository;
        UserSecuredRepository = userSecuredRepository;
        ServicePrincipalSecuredRepository = servicePrincipalSecuredRepository;
        GroupSecuredRepository = groupSecuredRepository;
    }

    protected virtual StateStoreRoleAssignment MapToEntity(StateStoreRoleAssignmentDto dto, Guid organizationId)
    {
        return StateStoreRoleAssignmentMapper.ToEntity(dto, organizationId);
    }

    protected virtual StateStoreRoleAssignmentDto MapToDto(StateStoreRoleAssignment entity)
    {
        return StateStoreRoleAssignmentMapper.ToDto(entity);
    }

    protected virtual void UpdateEntityFromDto(StateStoreRoleAssignment entity, StateStoreRoleAssignmentUpdateDto dto)
    {
        StateStoreRoleAssignmentMapper.UpdateEntity(entity, dto);
    }

    public virtual void Dispose()
    {
        BaseSecuredRepository.Dispose();
        UserSecuredRepository.Dispose();
        ServicePrincipalSecuredRepository.Dispose();
        GroupSecuredRepository.Dispose();
    }

    public virtual async Task<StateStoreRoleAssignmentDto> Get(Guid id, Guid organizationId)
    {
        var entity = await BaseSecuredRepository.Get(id, organizationId);
        return MapToDto(entity);
    }

    public virtual async Task<List<StateStoreRoleAssignmentDto>> List(Guid organizationId)
    {
        var entities = await BaseSecuredRepository.List(organizationId);
        return entities.Select(MapToDto).ToList();
    }

    public virtual async Task<StateStoreRoleAssignmentDto> Create(StateStoreRoleAssignmentDto dto, Guid organizationId)
    {
        var entity = MapToEntity(dto, organizationId);

        entity = dto.PrincipalDiscriminator switch
        {
            RoleAssignmentPrincipalDiscriminator.User => await UserSecuredRepository.Create((UserStateStoreRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.ServicePrincipal => await ServicePrincipalSecuredRepository.Create((ServicePrincipalStateStoreRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.Group => await GroupSecuredRepository.Create((GroupStateStoreRoleAssignment)entity),
            _ => throw new ArgumentException($"Unknown PrincipalDiscriminator: {dto.PrincipalDiscriminator}")
        };

        return MapToDto(entity);
    }

    public virtual async Task<StateStoreRoleAssignmentDto> Update(StateStoreRoleAssignmentUpdateDto dto, Guid id, Guid organizationId)
    {
        var entity = await BaseSecuredRepository.Get(id, organizationId);

        if (entity.PrincipalDiscriminator != dto.PrincipalDiscriminator)
            throw new InvalidOperationException(
                $"Cannot change PrincipalDiscriminator from {entity.PrincipalDiscriminator} to {dto.PrincipalDiscriminator}. " +
                "Delete the existing role assignment and create a new one instead.");

        UpdateEntityFromDto(entity, dto);

        entity = entity.PrincipalDiscriminator switch
        {
            RoleAssignmentPrincipalDiscriminator.User => await UserSecuredRepository.Update((UserStateStoreRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.ServicePrincipal => await ServicePrincipalSecuredRepository.Update((ServicePrincipalStateStoreRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.Group => await GroupSecuredRepository.Update((GroupStateStoreRoleAssignment)entity),
            _ => throw new ArgumentException($"Unknown PrincipalDiscriminator: {entity.PrincipalDiscriminator}")
        };

        return MapToDto(entity);
    }

    public virtual async Task Delete(Guid id, Guid organizationId)
    {
        await BaseSecuredRepository.Delete(id, organizationId);
    }
}
