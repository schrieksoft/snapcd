// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.Variables;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Variables;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.Variables;

public class VariableSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<VariableRepositorySettings> options)
{
    public VariableSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new VariableSecuredRepository(
            new VariableRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class VariableSecuredRepository : GenericSecuredRepository<
    Variable,
    VariableReadDto,
    VariableRepository,
    InputCreatedEvent,
    InputUpdatedEvent,
    InputDeletedEvent,
    VariableRepositorySettings>
{
    public VariableSecuredRepository(
        VariableRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader],
        StackRoles = [StackRole.Owner, StackRole.Contributor, StackRole.Reader],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor, NamespaceRole.Reader],
        ModuleRoles = [ModuleRole.Owner, ModuleRole.Reader],
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [],
        StackRoles = [],
        NamespaceRoles = [],
        ModuleRoles = [],
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [],
        StackRoles = [],
        NamespaceRoles = [],
        ModuleRoles = [],
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor],
        ModuleRoles = [ModuleRole.Owner],
    };

    public override IQueryable<Variable> ReadQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            ReadPermissionMap.OrganizationRoles,
            ReadPermissionMap.StackRoles,
            ReadPermissionMap.NamespaceRoles,
            ReadPermissionMap.ModuleRoles);
    }

    public override IQueryable<Variable> CreateQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            CreatePermissionMap.OrganizationRoles,
            CreatePermissionMap.StackRoles,
            CreatePermissionMap.NamespaceRoles,
            CreatePermissionMap.ModuleRoles);
    }

    public override IQueryable<Variable> UpdateQuery(Guid organizationId)
    {
        // Inputs can never be updated
        return Enumerable.Empty<Variable>().AsQueryable();
    }

    public override IQueryable<Variable> DeleteQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            DeletePermissionMap.OrganizationRoles,
            DeletePermissionMap.StackRoles,
            DeletePermissionMap.NamespaceRoles,
            DeletePermissionMap.ModuleRoles);
    }

    public override bool CanRead(Guid id, Guid organizationId)
    {
        return ReadQuery(organizationId).Any(e => e.Id == id && e.OrganizationId == organizationId);
    }

    public override bool CanCreate(Guid parentId, Guid organizationId)
    {
        // parentId is VariableSetId - find the module ID for this VariableSet
        var moduleId = Repository.DbContext.VariableSets
            .Where(s => s.Id == parentId && s.OrganizationId == organizationId)
            .Select(s => s.ModuleId)
            .FirstOrDefault();

        if (moduleId == Guid.Empty)
            return false;

        // Check if user can create entities for this module
        return CreateQuery(organizationId)
            .Join(Repository.DbContext.VariableSets,
                input => input.VariableSetId,
                variableSet => variableSet.Id,
                (input, variableSet) => variableSet)
            .Any(variableSet => variableSet.ModuleId == moduleId && variableSet.OrganizationId == organizationId);
    }

    public override bool CanUpdate(Guid id, Guid organizationId)
    {
        // Inputs can never be updated
        return false;
    }

    public override bool CanDelete(Guid id, Guid organizationId)
    {
        return DeleteQuery(organizationId).Any(e => e.Id == id && e.OrganizationId == organizationId);
    }

    public override string GetParentEntityName()
    {
        return "VariableSet";
    }

    private IQueryable<Variable> RoleQueryDispatch(
        Guid organizationId,
        List<OrganizationRole> organizationRoles,
        List<StackRole> stackRoles,
        List<NamespaceRole> namespaceRoles,
        List<ModuleRole> moduleRoles)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => RoleQuery<
                UserOrganizationRoleAssignment,
                UserStackRoleAssignment,
                UserNamespaceRoleAssignment,
                UserModuleRoleAssignment,
                UserGroupMember>(
                organizationId, principalId, organizationRoles, stackRoles, namespaceRoles, moduleRoles),
            PrincipalDiscriminator.ServicePrincipal => RoleQuery<
                ServicePrincipalOrganizationRoleAssignment,
                ServicePrincipalStackRoleAssignment,
                ServicePrincipalNamespaceRoleAssignment,
                ServicePrincipalModuleRoleAssignment,
                ServicePrincipalGroupMember>(
                organizationId, principalId, organizationRoles, stackRoles, namespaceRoles, moduleRoles),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }

    private IQueryable<Variable> RoleQuery<TOrganizationRoleAssignment, TStackRoleAssignment, TNamespaceRoleAssignment, TModuleRoleAssignment, TGroupMember>(
        Guid organizationId,
        Guid principalId,
        List<OrganizationRole> organizationRoles,
        List<StackRole> stackRoles,
        List<NamespaceRole> namespaceRoles,
        List<ModuleRole> moduleRoles)
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
        where TStackRoleAssignment : class, IStackRoleAssignment
        where TNamespaceRoleAssignment : class, INamespaceRoleAssignment
        where TModuleRoleAssignment : class, IModuleRoleAssignment
        where TGroupMember : class, IGroupMember
    {
        // Direct role assignments
        var entitiesFromModuleRoles =
            from input in Repository.DbContext.Variables
            join variableSet in Repository.DbContext.VariableSets
                on input.VariableSetId equals variableSet.Id
            join assignment in Repository.DbContext.Set<TModuleRoleAssignment>()
                on new { ModuleId = variableSet.ModuleId, variableSet.OrganizationId } equals new { assignment.ModuleId, assignment.OrganizationId }
            where input.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && moduleRoles.Contains(assignment.RoleName)
            select input;

        var entitiesFromNamespaceRoles =
            from input in Repository.DbContext.Variables
            join variableSet in Repository.DbContext.VariableSets
                on input.VariableSetId equals variableSet.Id
            join module in Repository.DbContext.Modules
                on new { ModuleId = variableSet.ModuleId, variableSet.OrganizationId } equals new { ModuleId = module.Id, module.OrganizationId }
            join assignment in Repository.DbContext.Set<TNamespaceRoleAssignment>()
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { assignment.NamespaceId, assignment.OrganizationId }
            where input.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && namespaceRoles.Contains(assignment.RoleName)
            select input;

        var entitiesFromStackRoles =
            from input in Repository.DbContext.Variables
            join variableSet in Repository.DbContext.VariableSets
                on input.VariableSetId equals variableSet.Id
            join module in Repository.DbContext.Modules
                on new { ModuleId = variableSet.ModuleId, variableSet.OrganizationId } equals new { ModuleId = module.Id, module.OrganizationId }
            join ns in Repository.DbContext.Namespaces
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
            join stack in Repository.DbContext.Stacks
                on new { StackId = ns.StackId, ns.OrganizationId } equals new { StackId = stack.Id, stack.OrganizationId }
            join assignment in Repository.DbContext.Set<TStackRoleAssignment>()
                on new { StackId = stack.Id, stack.OrganizationId } equals new { assignment.StackId, assignment.OrganizationId }
            where input.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && stackRoles.Contains(assignment.RoleName)
            select input;

        var entitiesFromOrganizationRoles =
            from input in Repository.DbContext.Variables
            join assignment in Repository.DbContext.Set<TOrganizationRoleAssignment>()
                on input.OrganizationId equals assignment.OrganizationId
            where input.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && organizationRoles.Contains(assignment.RoleName)
            select input;

        // Group-based role assignments
        var entitiesFromGroupModuleRoles = ModuleRolesFromGroupQuery<TGroupMember, TModuleRoleAssignment>(
            organizationId, principalId, moduleRoles);

        var entitiesFromGroupNamespaceRoles = NamespaceRolesFromGroupQuery<TGroupMember, TNamespaceRoleAssignment>(
            organizationId, principalId, namespaceRoles);

        var entitiesFromGroupStackRoles = StackRolesFromGroupQuery<TGroupMember, TStackRoleAssignment>(
            organizationId, principalId, stackRoles);

        var entitiesFromGroupOrganizationRoles = OrganizationRolesFromGroupQuery<TGroupMember, TOrganizationRoleAssignment>(
            organizationId, principalId, organizationRoles);

        return entitiesFromModuleRoles
            .Concat(entitiesFromNamespaceRoles)
            .Concat(entitiesFromStackRoles)
            .Concat(entitiesFromOrganizationRoles)
            .Concat(entitiesFromGroupModuleRoles)
            .Concat(entitiesFromGroupNamespaceRoles)
            .Concat(entitiesFromGroupStackRoles)
            .Concat(entitiesFromGroupOrganizationRoles);
    }

    private IQueryable<Variable> ModuleRolesFromGroupQuery<TGroupMember, TModuleRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<ModuleRole> moduleRoles)
        where TGroupMember : class, IGroupMember
        where TModuleRoleAssignment : class, IModuleRoleAssignment
    {
        return from input in Repository.DbContext.Variables
            join variableSet in Repository.DbContext.VariableSets
                on input.VariableSetId equals variableSet.Id
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                on input.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupModuleRoleAssignments
                on new { ModuleId = variableSet.ModuleId, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.ModuleId, assignment.OrganizationId, assignment.PrincipalId }
            where input.OrganizationId == organizationId
                  && groupMember.PrincipalId == principalId
                  && moduleRoles.Contains(assignment.RoleName)
            select input;
    }

    private IQueryable<Variable> NamespaceRolesFromGroupQuery<TGroupMember, TNamespaceRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<NamespaceRole> namespaceRoles)
        where TGroupMember : class, IGroupMember
        where TNamespaceRoleAssignment : class, INamespaceRoleAssignment
    {
        return from input in Repository.DbContext.Variables
            join variableSet in Repository.DbContext.VariableSets
                on input.VariableSetId equals variableSet.Id
            join module in Repository.DbContext.Modules
                on new { ModuleId = variableSet.ModuleId, variableSet.OrganizationId } equals new { ModuleId = module.Id, module.OrganizationId }
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                on input.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupNamespaceRoleAssignments
                on new { NamespaceId = module.NamespaceId, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.NamespaceId, assignment.OrganizationId, assignment.PrincipalId }
            where input.OrganizationId == organizationId
                  && groupMember.PrincipalId == principalId
                  && namespaceRoles.Contains(assignment.RoleName)
            select input;
    }

    private IQueryable<Variable> StackRolesFromGroupQuery<TGroupMember, TStackRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<StackRole> stackRoles)
        where TGroupMember : class, IGroupMember
        where TStackRoleAssignment : class, IStackRoleAssignment
    {
        return from input in Repository.DbContext.Variables
            join variableSet in Repository.DbContext.VariableSets
                on input.VariableSetId equals variableSet.Id
            join module in Repository.DbContext.Modules
                on new { ModuleId = variableSet.ModuleId, variableSet.OrganizationId } equals new { ModuleId = module.Id, module.OrganizationId }
            join ns in Repository.DbContext.Namespaces
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
            join stack in Repository.DbContext.Stacks
                on new { StackId = ns.StackId, ns.OrganizationId } equals new { StackId = stack.Id, stack.OrganizationId }
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                on input.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupStackRoleAssignments
                on new { StackId = stack.Id, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.StackId, assignment.OrganizationId, assignment.PrincipalId }
            where input.OrganizationId == organizationId
                  && groupMember.PrincipalId == principalId
                  && stackRoles.Contains(assignment.RoleName)
            select input;
    }

    private IQueryable<Variable> OrganizationRolesFromGroupQuery<TGroupMember, TOrganizationRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<OrganizationRole> organizationRoles)
        where TGroupMember : class, IGroupMember
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
    {
        return from input in Repository.DbContext.Variables
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                on input.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupOrganizationRoleAssignments
                on new { OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.OrganizationId, assignment.PrincipalId }
            where input.OrganizationId == organizationId
                  && groupMember.PrincipalId == principalId
                  && organizationRoles.Contains(assignment.RoleName)
            select input;
    }

    public async Task<List<Variable>> ListByVariableSetIds(List<Guid> variableSetIds, Guid organizationId)
    {
        var inputs = await Repository.ListByVariableSetIds(variableSetIds, organizationId);

        foreach (var input in inputs)
            if (!CanRead(input.Id, organizationId))
                throw new UnauthorizedAccessException($"Access denied to Input {input.Id}");

        return inputs;
    }

    public async Task<List<Variable>> ListByIds(List<Guid> inputIds, Guid organizationId)
    {
        var inputs = await Repository.ListByIds(inputIds, organizationId);

        foreach (var input in inputs)
            if (!CanRead(input.Id, organizationId))
                throw new UnauthorizedAccessException($"Access denied to Input {input.Id}");

        return inputs;
    }
}