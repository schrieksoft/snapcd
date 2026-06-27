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
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization.Base;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Interfaces;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;

public class GenericNamespaceChildSecuredRepositoryFactory<TEntity, TDto, TRepository, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions>(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<TOptions> options)
    where TEntity : class, IEntity, INamespaceChild
    where TRepository : GenericNamespaceChildRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions>
    where TCreateEvent : CreatedEvent<TDto>, new()
    where TUpdateEvent : UpdatedEvent<TDto>, new()
    where TDeleteEvent : DeletedEvent<TDto>, new()
    where TOptions : class, IEntitySettings
{
    public TRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return (TRepository)Activator.CreateInstance(typeof(TRepository), dbContext, principalProvider, bus, options)!;
    }
}

public abstract class
    GenericNamespaceChildSecuredRepository<TEntity, TDto, TRepository, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions> : GenericSecuredRepository<TEntity, TDto, TRepository, TCreateEvent,
    TUpdateEvent, TDeleteEvent, TOptions>
    where TEntity : class, IEntity, INamespaceChild
    where TRepository : GenericNamespaceChildRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions>
    where TCreateEvent : CreatedEvent<TDto>, new()
    where TUpdateEvent : UpdatedEvent<TDto>, new()
    where TDeleteEvent : DeletedEvent<TDto>, new()
    where TOptions : class, IEntitySettings

{
    public GenericNamespaceChildSecuredRepository(TRepository repository, IPrincipalProvider principalProvider) : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader, OrganizationRole.StackContributor, OrganizationRole.StackReader],
        StackRoles = [StackRole.Owner, StackRole.Contributor, StackRole.Reader],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor, NamespaceRole.Reader]
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StackContributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor]
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StackContributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.StackContributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor]
    };

    public override bool CanCreate(Guid parentId, Guid organizationId)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => CanCreateInNamespace<UserOrganizationRoleAssignment, UserStackRoleAssignment, UserNamespaceRoleAssignment, UserGroupMember>(
                organizationId, principalId, parentId),
            PrincipalDiscriminator.ServicePrincipal => CanCreateInNamespace<ServicePrincipalOrganizationRoleAssignment, ServicePrincipalStackRoleAssignment, ServicePrincipalNamespaceRoleAssignment,
                ServicePrincipalGroupMember>(
                organizationId, principalId, parentId),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }

    public override bool CanRead(Guid id, Guid organizationId)
    {
        return ReadQuery(organizationId).Any(e => e.Id == id && e.OrganizationId == organizationId);
    }

    public override bool CanUpdate(Guid id, Guid organizationId)
    {
        return UpdateQuery(organizationId).Any(e => e.Id == id && e.OrganizationId == organizationId);
    }

    public override bool CanDelete(Guid id, Guid organizationId)
    {
        return DeleteQuery(organizationId).Any(e => e.Id == id && e.OrganizationId == organizationId);
    }

    public override IQueryable<TEntity> CreateQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            CreatePermissionMap.OrganizationRoles,
            CreatePermissionMap.StackRoles,
            CreatePermissionMap.NamespaceRoles,
            []);
    }

    public override IQueryable<TEntity> ReadQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            ReadPermissionMap.OrganizationRoles,
            ReadPermissionMap.StackRoles,
            ReadPermissionMap.NamespaceRoles,
            []);
    }

    public override IQueryable<TEntity> UpdateQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            UpdatePermissionMap.OrganizationRoles,
            UpdatePermissionMap.StackRoles,
            UpdatePermissionMap.NamespaceRoles,
            []);
    }

    public override IQueryable<TEntity> DeleteQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            DeletePermissionMap.OrganizationRoles,
            DeletePermissionMap.StackRoles,
            DeletePermissionMap.NamespaceRoles,
            []);
    }

    public override string GetParentEntityName()
    {
        return "Namespace";
    }

    protected virtual IQueryable<TEntity> RoleQueryDispatch(
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


    protected IQueryable<TEntity> RoleQuery<TOrganizationRoleAssignment, TStackRoleAssignment, TNamespaceRoleAssignment, TModuleRoleAssignment, TGroupMember>(
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
        var modulesFromNamespaceRoles =
            from module in Repository.DbContext.Set<TEntity>()
            join ns in Repository.DbContext.Namespaces
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
            join assignment in Repository.DbContext.Set<TNamespaceRoleAssignment>()
                on new { NamespaceId = ns.Id, ns.OrganizationId } equals new { assignment.NamespaceId, assignment.OrganizationId }
            where module.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && namespaceRoles.Contains(assignment.RoleName)
            select module;

        var modulesFromStackRoles =
            from module in Repository.DbContext.Set<TEntity>()
            join ns in Repository.DbContext.Namespaces
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
            join stack in Repository.DbContext.Stacks
                on new { StackId = ns.StackId, ns.OrganizationId } equals new { StackId = stack.Id, stack.OrganizationId }
            join assignment in Repository.DbContext.Set<TStackRoleAssignment>()
                on new { StackId = stack.Id, stack.OrganizationId } equals new { assignment.StackId, assignment.OrganizationId }
            where module.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && stackRoles.Contains(assignment.RoleName)
            select module;

        var modulesFromOrganizationRoles =
            from module in Repository.DbContext.Set<TEntity>()
            join assignment in Repository.DbContext.Set<TOrganizationRoleAssignment>()
                on module.OrganizationId equals assignment.OrganizationId
            where module.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && organizationRoles.Contains(assignment.RoleName)
            select module;

        var modulesFromModuleRoles =
            from entity in Repository.DbContext.Set<TEntity>()
            join ns in Repository.DbContext.Namespaces
                on new { NamespaceId = entity.NamespaceId, entity.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
            join mod in Repository.DbContext.Modules
                on new { NamespaceId = ns.Id, ns.OrganizationId } equals new { mod.NamespaceId, mod.OrganizationId }
            join assignment in Repository.DbContext.Set<TModuleRoleAssignment>()
                on new { ModuleId = mod.Id, mod.OrganizationId } equals new { assignment.ModuleId, assignment.OrganizationId }
            where entity.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && moduleRoles.Contains(assignment.RoleName)
            select entity;

        // Group-based role assignments
        var modulesFromGroupNamespaceRoles = NamespaceRolesFromGroupQuery<TGroupMember, TNamespaceRoleAssignment>(
            organizationId, principalId, namespaceRoles);

        var modulesFromGroupStackRoles = StackRolesFromGroupQuery<TGroupMember, TStackRoleAssignment>(
            organizationId, principalId, stackRoles);

        var modulesFromGroupOrganizationRoles = OrganizationRolesFromGroupQuery<TGroupMember, TOrganizationRoleAssignment>(
            organizationId, principalId, organizationRoles);

        var modulesFromGroupModuleRoles = ModuleRolesFromGroupQuery<TGroupMember, TModuleRoleAssignment>(
            organizationId, principalId, moduleRoles);


        return modulesFromNamespaceRoles
            .Concat(modulesFromStackRoles)
            .Concat(modulesFromOrganizationRoles)
            .Concat(modulesFromModuleRoles)
            .Concat(modulesFromGroupNamespaceRoles)
            .Concat(modulesFromGroupStackRoles)
            .Concat(modulesFromGroupOrganizationRoles)
            .Concat(modulesFromGroupModuleRoles);
    }

    private IQueryable<TEntity> NamespaceRolesFromGroupQuery<TGroupMember, TNamespaceRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<NamespaceRole> namespaceRoles)
        where TGroupMember : class, IGroupMember
        where TNamespaceRoleAssignment : class, INamespaceRoleAssignment
    {
        return from module in Repository.DbContext.Set<TEntity>()
            join ns in Repository.DbContext.Namespaces
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
                on module.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupNamespaceRoleAssignments
                on new { NamespaceId = ns.Id, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.NamespaceId, assignment.OrganizationId, assignment.PrincipalId }
            where module.OrganizationId == organizationId
                  && namespaceRoles.Contains(assignment.RoleName)
            select module;
    }

    private IQueryable<TEntity> StackRolesFromGroupQuery<TGroupMember, TStackRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<StackRole> stackRoles)
        where TGroupMember : class, IGroupMember
        where TStackRoleAssignment : class, IStackRoleAssignment
    {
        return from module in Repository.DbContext.Set<TEntity>()
            join ns in Repository.DbContext.Namespaces
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
            join stack in Repository.DbContext.Stacks
                on new { StackId = ns.StackId, ns.OrganizationId } equals new { StackId = stack.Id, stack.OrganizationId }
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
                on module.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupStackRoleAssignments
                on new { StackId = stack.Id, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.StackId, assignment.OrganizationId, assignment.PrincipalId }
            where module.OrganizationId == organizationId
                  && stackRoles.Contains(assignment.RoleName)
            select module;
    }

    private IQueryable<TEntity> OrganizationRolesFromGroupQuery<TGroupMember, TOrganizationRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<OrganizationRole> organizationRoles)
        where TGroupMember : class, IGroupMember
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
    {
        return from module in Repository.DbContext.Set<TEntity>()
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
                on module.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupOrganizationRoleAssignments
                on new { OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.OrganizationId, assignment.PrincipalId }
            where module.OrganizationId == organizationId
                  && organizationRoles.Contains(assignment.RoleName)
            select module;
    }

    private IQueryable<TEntity> ModuleRolesFromGroupQuery<TGroupMember, TModuleRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<ModuleRole> moduleRoles)
        where TGroupMember : class, IGroupMember
        where TModuleRoleAssignment : class, IModuleRoleAssignment
    {
        return from entity in Repository.DbContext.Set<TEntity>()
            join ns in Repository.DbContext.Namespaces
                on new { NamespaceId = entity.NamespaceId, entity.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
            join mod in Repository.DbContext.Modules
                on new { NamespaceId = ns.Id, ns.OrganizationId } equals new { mod.NamespaceId, mod.OrganizationId }
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
                on entity.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupModuleRoleAssignments
                on new { ModuleId = mod.Id, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.ModuleId, assignment.OrganizationId, assignment.PrincipalId }
            where entity.OrganizationId == organizationId
                  && moduleRoles.Contains(assignment.RoleName)
            select entity;
    }

    /// <summary>
    /// Returns a query for namespaces where the principal can create entities.
    /// This checks if the principal has create permissions via org/stack/namespace roles.
    /// </summary>
    protected IQueryable<Namespace> NamespaceRoleQuery<TOrganizationRoleAssignment, TStackRoleAssignment, TNamespaceRoleAssignment, TGroupMember>(
        Guid organizationId,
        Guid principalId)
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
        where TStackRoleAssignment : class, IStackRoleAssignment
        where TNamespaceRoleAssignment : class, INamespaceRoleAssignment
        where TGroupMember : class, IGroupMember
    {
        // Direct organization role assignment
        var namespacesFromOrgPermission =
            from ns in Repository.DbContext.Namespaces
            where ns.OrganizationId == organizationId
            join assignment in Repository.DbContext.Set<TOrganizationRoleAssignment>()
                on ns.OrganizationId equals assignment.OrganizationId
            where assignment.PrincipalId == principalId
                  && (assignment.RoleName == OrganizationRole.Owner || assignment.RoleName == OrganizationRole.Contributor)
            select ns;

        // Group-based organization role assignment
        var namespacesFromOrgPermissionViaGroup =
            from ns in Repository.DbContext.Namespaces
            where ns.OrganizationId == organizationId
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
                on ns.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupOrganizationRoleAssignments
                on new { OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.OrganizationId, assignment.PrincipalId }
            where assignment.RoleName == OrganizationRole.Owner || assignment.RoleName == OrganizationRole.Contributor
            select ns;

        // Direct stack role assignment
        var namespacesFromStackPermission =
            from ns in Repository.DbContext.Namespaces
            where ns.OrganizationId == organizationId
            join assignment in Repository.DbContext.Set<TStackRoleAssignment>()
                on new { StackId = ns.StackId, ns.OrganizationId } equals new { assignment.StackId, assignment.OrganizationId }
            where assignment.PrincipalId == principalId
                  && (assignment.RoleName == StackRole.Owner || assignment.RoleName == StackRole.Contributor)
            select ns;

        // Group-based stack role assignment
        var namespacesFromStackPermissionViaGroup =
            from ns in Repository.DbContext.Namespaces
            where ns.OrganizationId == organizationId
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
                on ns.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupStackRoleAssignments
                on new { StackId = ns.StackId, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.StackId, assignment.OrganizationId, assignment.PrincipalId }
            where assignment.RoleName == StackRole.Owner || assignment.RoleName == StackRole.Contributor
            select ns;

        // Direct namespace role assignment
        var namespacesFromNamespacePermission =
            from ns in Repository.DbContext.Namespaces
            where ns.OrganizationId == organizationId
            join assignment in Repository.DbContext.Set<TNamespaceRoleAssignment>()
                on new { NamespaceId = ns.Id, ns.OrganizationId } equals new { assignment.NamespaceId, assignment.OrganizationId }
            where assignment.PrincipalId == principalId
                  && (assignment.RoleName == NamespaceRole.Owner || assignment.RoleName == NamespaceRole.Contributor)
            select ns;

        // Group-based namespace role assignment
        var namespacesFromNamespacePermissionViaGroup =
            from ns in Repository.DbContext.Namespaces
            where ns.OrganizationId == organizationId
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                .Where(gm => gm.PrincipalId == principalId && gm.OrganizationId == organizationId)
                on ns.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupNamespaceRoleAssignments
                on new { NamespaceId = ns.Id, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.NamespaceId, assignment.OrganizationId, assignment.PrincipalId }
            where assignment.RoleName == NamespaceRole.Owner || assignment.RoleName == NamespaceRole.Contributor
            select ns;


#if DEBUG
        // Materialize queries for debugging inspection
        var debugOrgPermission = namespacesFromOrgPermission.ToList();
        var debugOrgPermissionViaGroup = namespacesFromOrgPermissionViaGroup.ToList();
        var debugStackPermission = namespacesFromStackPermission.ToList();
        var debugStackPermissionViaGroup = namespacesFromStackPermissionViaGroup.ToList();
        var debugNamespacePermission = namespacesFromNamespacePermission.ToList();
        var debugNamespacePermissionViaGroup = namespacesFromNamespacePermissionViaGroup.ToList();
#endif

        return namespacesFromOrgPermission
            .Concat(namespacesFromOrgPermissionViaGroup)
            .Concat(namespacesFromStackPermission)
            .Concat(namespacesFromStackPermissionViaGroup)
            .Concat(namespacesFromNamespacePermission)
            .Concat(namespacesFromNamespacePermissionViaGroup);
    }

    /// <summary>
    /// Checks if the principal can create entities in the specified namespace.
    /// </summary>
    protected bool CanCreateInNamespace<TOrganizationRoleAssignment, TStackRoleAssignment, TNamespaceRoleAssignment, TGroupMember>(
        Guid organizationId,
        Guid principalId,
        Guid namespaceId)
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
        where TStackRoleAssignment : class, IStackRoleAssignment
        where TNamespaceRoleAssignment : class, INamespaceRoleAssignment
        where TGroupMember : class, IGroupMember
    {
        return NamespaceRoleQuery<TOrganizationRoleAssignment, TStackRoleAssignment, TNamespaceRoleAssignment, TGroupMember>(
            organizationId, principalId).Any(ns => ns.Id == namespaceId && ns.OrganizationId == organizationId);
    }
}