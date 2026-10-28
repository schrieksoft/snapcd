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

public class NamespaceRoleAssignmentServiceFactory(
    NamespaceRoleAssignmentSecuredRepositoryFactory namespaceRoleAssignmentSecuredRepositoryFactory,
    UserNamespaceRoleAssignmentSecuredRepositoryFactory userNamespaceSecuredRepositoryFactory,
    ServicePrincipalNamespaceRoleAssignmentSecuredRepositoryFactory servicePrincipalNamespaceSecuredRepositoryFactory,
    GroupNamespaceRoleAssignmentSecuredRepositoryFactory groupNamespaceSecuredRepositoryFactory)
{
    public NamespaceRoleAssignmentService Create(IPrincipalProvider? principalProvider = null)
    {
        var baseRepo = namespaceRoleAssignmentSecuredRepositoryFactory.Create(principalProvider);
        var userRepo = userNamespaceSecuredRepositoryFactory.Create(principalProvider);
        var servicePrincipalRepo = servicePrincipalNamespaceSecuredRepositoryFactory.Create(principalProvider);
        var groupRepo = groupNamespaceSecuredRepositoryFactory.Create(principalProvider);

        return new NamespaceRoleAssignmentService(
            baseRepo,
            userRepo,
            servicePrincipalRepo,
            groupRepo);
    }
}

public class NamespaceRoleAssignmentService : IDisposable
{
    protected readonly NamespaceRoleAssignmentSecuredRepository BaseSecuredRepository;
    protected readonly UserNamespaceRoleAssignmentSecuredRepository UserSecuredRepository;
    protected readonly ServicePrincipalNamespaceRoleAssignmentSecuredRepository ServicePrincipalSecuredRepository;
    protected readonly GroupNamespaceRoleAssignmentSecuredRepository GroupSecuredRepository;

    public NamespaceRoleAssignmentService(
        NamespaceRoleAssignmentSecuredRepository baseSecuredRepository,
        UserNamespaceRoleAssignmentSecuredRepository userSecuredRepository,
        ServicePrincipalNamespaceRoleAssignmentSecuredRepository servicePrincipalSecuredRepository,
        GroupNamespaceRoleAssignmentSecuredRepository groupSecuredRepository)
    {
        BaseSecuredRepository = baseSecuredRepository;
        UserSecuredRepository = userSecuredRepository;
        ServicePrincipalSecuredRepository = servicePrincipalSecuredRepository;
        GroupSecuredRepository = groupSecuredRepository;
    }

    protected virtual NamespaceRoleAssignment MapToEntity(NamespaceRoleAssignmentReadDto dto, Guid organizationId)
    {
        return NamespaceRoleAssignmentMapper.ToEntity(dto, organizationId);
    }

    protected virtual NamespaceRoleAssignmentReadDto MapToDto(NamespaceRoleAssignment entity)
    {
        return NamespaceRoleAssignmentMapper.ToDto(entity);
    }

    protected virtual void UpdateEntityFromDto(NamespaceRoleAssignment entity, NamespaceRoleAssignmentUpdateDto dto)
    {
        NamespaceRoleAssignmentMapper.UpdateEntity(entity, dto);
    }

    public virtual void Dispose()
    {
        BaseSecuredRepository.Dispose();
        UserSecuredRepository.Dispose();
        ServicePrincipalSecuredRepository.Dispose();
        GroupSecuredRepository.Dispose();
    }

    public virtual async Task<NamespaceRoleAssignmentReadDto> Get(Guid id, Guid organizationId)
    {
        var entity = await BaseSecuredRepository.Get(id, organizationId);
        return MapToDto(entity);
    }

    public virtual async Task<List<NamespaceRoleAssignmentReadDto>> List(Guid organizationId)
    {
        var entities = await BaseSecuredRepository.List(organizationId);
        return entities.Select(MapToDto).ToList();
    }

    public virtual async Task<NamespaceRoleAssignmentReadDto> Create(NamespaceRoleAssignmentReadDto dto, Guid organizationId)
    {
        var entity = MapToEntity(dto, organizationId);

        entity = dto.PrincipalDiscriminator switch
        {
            RoleAssignmentPrincipalDiscriminator.User => await UserSecuredRepository.Create((UserNamespaceRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.ServicePrincipal => await ServicePrincipalSecuredRepository.Create((ServicePrincipalNamespaceRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.Group => await GroupSecuredRepository.Create((GroupNamespaceRoleAssignment)entity),
            _ => throw new ArgumentException($"Unknown PrincipalDiscriminator: {dto.PrincipalDiscriminator}")
        };

        return MapToDto(entity);
    }

    public virtual async Task<NamespaceRoleAssignmentReadDto> Update(NamespaceRoleAssignmentUpdateDto dto, Guid id, Guid organizationId)
    {
        var entity = await BaseSecuredRepository.Get(id, organizationId);

        if (entity.PrincipalDiscriminator != dto.PrincipalDiscriminator)
            throw new InvalidOperationException(
                $"Cannot change PrincipalDiscriminator from {entity.PrincipalDiscriminator} to {dto.PrincipalDiscriminator}. " +
                "Delete the existing role assignment and create a new one instead.");

        UpdateEntityFromDto(entity, dto);

        entity = entity.PrincipalDiscriminator switch
        {
            RoleAssignmentPrincipalDiscriminator.User => await UserSecuredRepository.Update((UserNamespaceRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.ServicePrincipal => await ServicePrincipalSecuredRepository.Update((ServicePrincipalNamespaceRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.Group => await GroupSecuredRepository.Update((GroupNamespaceRoleAssignment)entity),
            _ => throw new ArgumentException($"Unknown PrincipalDiscriminator: {entity.PrincipalDiscriminator}")
        };

        return MapToDto(entity);
    }

    public virtual async Task Delete(Guid id, Guid organizationId)
    {
        await BaseSecuredRepository.Delete(id, organizationId);
    }
}