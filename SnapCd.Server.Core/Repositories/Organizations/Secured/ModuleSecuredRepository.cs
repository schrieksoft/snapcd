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
using SnapCd.Contracts.Dto.Modules;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class ModuleSecuredRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ModuleRepositorySettings> options)
{
    public ModuleSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleSecuredRepository(new ModuleRepository(dbContext, principalProvider, bus, options), principalProvider);
    }
}

public class ModuleSecuredRepository : GenericNamespaceChildSecuredRepository<Module, ModuleReadDto, ModuleRepository, ModuleCreatedEvent, ModuleUpdatedEvent, ModuleDeletedEvent, ModuleRepositorySettings>
{
    public ModuleSecuredRepository(ModuleRepository moduleRepository, IPrincipalProvider principalProvider) : base(moduleRepository, principalProvider)
    {
    }

    # region overrides

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StackContributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor, NamespaceRole.ModuleCreator]
    };

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader, OrganizationRole.StackContributor, OrganizationRole.StackReader],
        StackRoles = [StackRole.Owner, StackRole.Contributor, StackRole.Reader],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor, NamespaceRole.Reader],
        ModuleRoles = [ModuleRole.Owner, ModuleRole.Reader]
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StackContributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor],
        ModuleRoles = [ModuleRole.Owner]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StackContributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor],
        ModuleRoles = [ModuleRole.Owner]
    };

    public override bool CanCreate(Guid parentId, Guid organizationId)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => CanCreateInNamespaceWithModuleCreator<UserOrganizationRoleAssignment, UserStackRoleAssignment, UserNamespaceRoleAssignment>(
                organizationId, principalId, parentId),
            PrincipalDiscriminator.ServicePrincipal => CanCreateInNamespaceWithModuleCreator<ServicePrincipalOrganizationRoleAssignment, ServicePrincipalStackRoleAssignment,
                ServicePrincipalNamespaceRoleAssignment>(
                organizationId, principalId, parentId),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }


    public override IQueryable<Module> CreateQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            CreatePermissionMap.OrganizationRoles,
            CreatePermissionMap.StackRoles,
            CreatePermissionMap.NamespaceRoles,
            CreatePermissionMap.ModuleRoles
        );
    }

    public override IQueryable<Module> ReadQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            ReadPermissionMap.OrganizationRoles,
            ReadPermissionMap.StackRoles,
            ReadPermissionMap.NamespaceRoles,
            ReadPermissionMap.ModuleRoles
        );
    }

    public override IQueryable<Module> UpdateQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            UpdatePermissionMap.OrganizationRoles,
            UpdatePermissionMap.StackRoles,
            UpdatePermissionMap.NamespaceRoles,
            UpdatePermissionMap.ModuleRoles
        );
    }

    public override IQueryable<Module> DeleteQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            DeletePermissionMap.OrganizationRoles,
            DeletePermissionMap.StackRoles,
            DeletePermissionMap.NamespaceRoles,
            DeletePermissionMap.ModuleRoles
        );
    }

    # endregion


    # region private

    protected override IQueryable<Module> RoleQueryDispatch(
        Guid organizationId,
        List<OrganizationRole> organizationRoles,
        List<StackRole> stackRoles,
        List<NamespaceRole> namespaceRoles,
        List<ModuleRole> moduleRoles)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        var roleQueryBase = base.RoleQueryDispatch(organizationId, organizationRoles, stackRoles, namespaceRoles, []);

        var roleQueryModule = PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => RoleQuery<UserModuleRoleAssignment>(organizationId, principalId, moduleRoles),
            PrincipalDiscriminator.ServicePrincipal => RoleQuery<ServicePrincipalModuleRoleAssignment>(organizationId, principalId, moduleRoles),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };

        return roleQueryBase.Concat(roleQueryModule);
    }


    private IQueryable<Module> RoleQuery<TModuleRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<ModuleRole> moduleRoles
    )
        where TModuleRoleAssignment : class, IModuleRoleAssignment

    {
        // Role assignment on Module
        var modulesFromModuleRoles =
            from module in Repository.DbContext.Modules
            join assignment in Repository.DbContext.Set<TModuleRoleAssignment>()
                on new { ModuleId = module.Id, module.OrganizationId } equals new { assignment.ModuleId, assignment.OrganizationId }
            where module.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && moduleRoles.Contains(assignment.RoleName)
            select module;

        // Union all three queries
        return modulesFromModuleRoles;
    }


    private bool CanCreateInNamespaceWithModuleCreator<TOrganizationRoleAssignment, TStackRoleAssignment, TNamespaceRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        Guid namespaceId)
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
        where TStackRoleAssignment : class, IStackRoleAssignment
        where TNamespaceRoleAssignment : class, INamespaceRoleAssignment
    {
        // Check direct organization role assignment
        var hasOrgPermission = Repository.DbContext.Set<TOrganizationRoleAssignment>()
            .Any(ra => ra.OrganizationId == organizationId
                       && ra.PrincipalId == principalId
                       && CreatePermissionMap.OrganizationRoles.Contains(ra.RoleName));

        if (hasOrgPermission)
            return true;

        // Check group-based organization role assignment
        var hasOrgPermissionViaGroup = PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => (
                from gum in Repository.DbContext.UserGroupMembers
                where gum.UserId == principalId && gum.OrganizationId == organizationId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gum.GroupId, RootOrganizationId = gum.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupOrganizationRoleAssignments
                    on new { OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where CreatePermissionMap.OrganizationRoles.Contains(assignment.RoleName)
                select assignment
            ).Any(),
            PrincipalDiscriminator.ServicePrincipal => (
                from gspm in Repository.DbContext.ServicePrincipalGroupMembers
                where gspm.ServicePrincipalId == principalId && gspm.OrganizationId == organizationId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gspm.GroupId, RootOrganizationId = gspm.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupOrganizationRoleAssignments
                    on new { OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where CreatePermissionMap.OrganizationRoles.Contains(assignment.RoleName)
                select assignment
            ).Any(),
            _ => false
        };

        if (hasOrgPermissionViaGroup)
            return true;

        // Check direct stack role assignment
        var hasStackPermission = (
            from ns in Repository.DbContext.Namespaces
            where ns.Id == namespaceId && ns.OrganizationId == organizationId
            join stack in Repository.DbContext.Stacks
                on new { StackId = ns.StackId, ns.OrganizationId } equals new { StackId = stack.Id, stack.OrganizationId }
            join assignment in Repository.DbContext.Set<TStackRoleAssignment>()
                on new { StackId = stack.Id, stack.OrganizationId } equals new { assignment.StackId, assignment.OrganizationId }
            where assignment.PrincipalId == principalId
                  && CreatePermissionMap.StackRoles.Contains(assignment.RoleName)
            select stack
        ).Any();

        if (hasStackPermission)
            return true;

        // Check group-based stack role assignment
        var hasStackPermissionViaGroup = PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => (
                from ns in Repository.DbContext.Namespaces
                where ns.Id == namespaceId && ns.OrganizationId == organizationId
                join stack in Repository.DbContext.Stacks
                    on new { StackId = ns.StackId, ns.OrganizationId } equals new { StackId = stack.Id, stack.OrganizationId }
                join gum in Repository.DbContext.UserGroupMembers
                    on organizationId equals gum.OrganizationId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gum.GroupId, RootOrganizationId = gum.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupStackRoleAssignments
                    on new { StackId = stack.Id, OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.StackId, assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where gum.UserId == principalId
                      && CreatePermissionMap.StackRoles.Contains(assignment.RoleName)
                select assignment
            ).Any(),
            PrincipalDiscriminator.ServicePrincipal => (
                from ns in Repository.DbContext.Namespaces
                where ns.Id == namespaceId && ns.OrganizationId == organizationId
                join stack in Repository.DbContext.Stacks
                    on new { StackId = ns.StackId, ns.OrganizationId } equals new { StackId = stack.Id, stack.OrganizationId }
                join gspm in Repository.DbContext.ServicePrincipalGroupMembers
                    on organizationId equals gspm.OrganizationId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gspm.GroupId, RootOrganizationId = gspm.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupStackRoleAssignments
                    on new { StackId = stack.Id, OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.StackId, assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where gspm.ServicePrincipalId == principalId
                      && CreatePermissionMap.StackRoles.Contains(assignment.RoleName)
                select assignment
            ).Any(),
            _ => false
        };

        if (hasStackPermissionViaGroup)
            return true;

        // Check direct namespace role assignment
        var hasNamespacePermission = Repository.DbContext.Set<TNamespaceRoleAssignment>()
            .Any(ra => ra.NamespaceId == namespaceId
                       && ra.OrganizationId == organizationId
                       && ra.PrincipalId == principalId
                       && CreatePermissionMap.NamespaceRoles.Contains(ra.RoleName));

        if (hasNamespacePermission)
            return true;

        // Check group-based namespace role assignment
        var hasNamespacePermissionViaGroup = PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => (
                from gum in Repository.DbContext.UserGroupMembers
                where gum.UserId == principalId && gum.OrganizationId == organizationId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gum.GroupId, RootOrganizationId = gum.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupNamespaceRoleAssignments
                    on new { NamespaceId = namespaceId, OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.NamespaceId, assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where CreatePermissionMap.NamespaceRoles.Contains(assignment.RoleName)
                select assignment
            ).Any(),
            PrincipalDiscriminator.ServicePrincipal => (
                from gspm in Repository.DbContext.ServicePrincipalGroupMembers
                where gspm.ServicePrincipalId == principalId && gspm.OrganizationId == organizationId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gspm.GroupId, RootOrganizationId = gspm.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupNamespaceRoleAssignments
                    on new { NamespaceId = namespaceId, OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.NamespaceId, assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where CreatePermissionMap.NamespaceRoles.Contains(assignment.RoleName)
                select assignment
            ).Any(),
            _ => false
        };

        return hasNamespacePermissionViaGroup;
    }

    # endregion

    # region public methods

    public async Task<Module> Get(Guid namespaceId, string name, Guid organizationId)
    {
        var module = await Repository.Get(namespaceId, name, organizationId);

        if (!CanRead(module.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to module {module.Id}");

        return module;
    }

    public async Task<Module> Get(string stackName, string namespaceName, string moduleName, Guid organizationId)
    {
        var module = await Repository.Get(stackName, namespaceName, moduleName, organizationId);

        if (!CanRead(module.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to module {module.Id}");

        return module;
    }

    # endregion
}