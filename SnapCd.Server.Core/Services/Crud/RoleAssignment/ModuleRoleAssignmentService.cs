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

public class ModuleRoleAssignmentServiceFactory(
    ModuleRoleAssignmentSecuredRepositoryFactory moduleRoleAssignmentSecuredRepositoryFactory,
    UserModuleRoleAssignmentSecuredRepositoryFactory userModuleSecuredRepositoryFactory,
    ServicePrincipalModuleRoleAssignmentSecuredRepositoryFactory servicePrincipalModuleSecuredRepositoryFactory,
    GroupModuleRoleAssignmentSecuredRepositoryFactory groupModuleSecuredRepositoryFactory)
{
    public ModuleRoleAssignmentService Create(IPrincipalProvider? principalProvider = null)
    {
        var baseRepo = moduleRoleAssignmentSecuredRepositoryFactory.Create(principalProvider);
        var userRepo = userModuleSecuredRepositoryFactory.Create(principalProvider);
        var servicePrincipalRepo = servicePrincipalModuleSecuredRepositoryFactory.Create(principalProvider);
        var groupRepo = groupModuleSecuredRepositoryFactory.Create(principalProvider);

        return new ModuleRoleAssignmentService(
            baseRepo,
            userRepo,
            servicePrincipalRepo,
            groupRepo);
    }
}

public class ModuleRoleAssignmentService : IDisposable
{
    protected readonly ModuleRoleAssignmentSecuredRepository BaseSecuredRepository;
    protected readonly UserModuleRoleAssignmentSecuredRepository UserSecuredRepository;
    protected readonly ServicePrincipalModuleRoleAssignmentSecuredRepository ServicePrincipalSecuredRepository;
    protected readonly GroupModuleRoleAssignmentSecuredRepository GroupSecuredRepository;

    public ModuleRoleAssignmentService(
        ModuleRoleAssignmentSecuredRepository baseSecuredRepository,
        UserModuleRoleAssignmentSecuredRepository userSecuredRepository,
        ServicePrincipalModuleRoleAssignmentSecuredRepository servicePrincipalSecuredRepository,
        GroupModuleRoleAssignmentSecuredRepository groupSecuredRepository)
    {
        BaseSecuredRepository = baseSecuredRepository;
        UserSecuredRepository = userSecuredRepository;
        ServicePrincipalSecuredRepository = servicePrincipalSecuredRepository;
        GroupSecuredRepository = groupSecuredRepository;
    }

    protected virtual ModuleRoleAssignment MapToEntity(ModuleRoleAssignmentReadDto dto, Guid organizationId)
    {
        return ModuleRoleAssignmentMapper.ToEntity(dto, organizationId);
    }

    protected virtual ModuleRoleAssignmentReadDto MapToDto(ModuleRoleAssignment entity)
    {
        return ModuleRoleAssignmentMapper.ToDto(entity);
    }

    protected virtual void UpdateEntityFromDto(ModuleRoleAssignment entity, ModuleRoleAssignmentUpdateDto dto)
    {
        ModuleRoleAssignmentMapper.UpdateEntity(entity, dto);
    }

    public virtual void Dispose()
    {
        BaseSecuredRepository.Dispose();
        UserSecuredRepository.Dispose();
        ServicePrincipalSecuredRepository.Dispose();
        GroupSecuredRepository.Dispose();
    }

    public virtual async Task<ModuleRoleAssignmentReadDto> Get(Guid id, Guid organizationId)
    {
        var entity = await BaseSecuredRepository.Get(id, organizationId);
        return MapToDto(entity);
    }

    public virtual async Task<List<ModuleRoleAssignmentReadDto>> List(Guid organizationId)
    {
        var entities = await BaseSecuredRepository.List(organizationId);
        return entities.Select(MapToDto).ToList();
    }

    public virtual async Task<ModuleRoleAssignmentReadDto> Create(ModuleRoleAssignmentReadDto dto, Guid organizationId)
    {
        var entity = MapToEntity(dto, organizationId);

        entity = dto.PrincipalDiscriminator switch
        {
            RoleAssignmentPrincipalDiscriminator.User => await UserSecuredRepository.Create((UserModuleRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.ServicePrincipal => await ServicePrincipalSecuredRepository.Create((ServicePrincipalModuleRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.Group => await GroupSecuredRepository.Create((GroupModuleRoleAssignment)entity),
            _ => throw new ArgumentException($"Unknown PrincipalDiscriminator: {dto.PrincipalDiscriminator}")
        };

        return MapToDto(entity);
    }

    public virtual async Task<ModuleRoleAssignmentReadDto> Update(ModuleRoleAssignmentUpdateDto dto, Guid id, Guid organizationId)
    {
        var entity = await BaseSecuredRepository.Get(id, organizationId);

        if (entity.PrincipalDiscriminator != dto.PrincipalDiscriminator)
            throw new InvalidOperationException(
                $"Cannot change PrincipalDiscriminator from {entity.PrincipalDiscriminator} to {dto.PrincipalDiscriminator}. " +
                "Delete the existing role assignment and create a new one instead.");

        UpdateEntityFromDto(entity, dto);

        entity = entity.PrincipalDiscriminator switch
        {
            RoleAssignmentPrincipalDiscriminator.User => await UserSecuredRepository.Update((UserModuleRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.ServicePrincipal => await ServicePrincipalSecuredRepository.Update((ServicePrincipalModuleRoleAssignment)entity),
            RoleAssignmentPrincipalDiscriminator.Group => await GroupSecuredRepository.Update((GroupModuleRoleAssignment)entity),
            _ => throw new ArgumentException($"Unknown PrincipalDiscriminator: {entity.PrincipalDiscriminator}")
        };

        return MapToDto(entity);
    }

    public virtual async Task Delete(Guid id, Guid organizationId)
    {
        await BaseSecuredRepository.Delete(id, organizationId);
    }
}