using SnapCd.Contracts;
using SnapCd.Server.Core.Entities.Definition.GroupMembers;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization.Base;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Interfaces;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;

public abstract class GenericStackChildSecuredRepository<TEntity, TDto, TRepository, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions> :
    GenericSecuredRepository<TEntity, TDto, TRepository, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions>
    where TEntity : class, IEntity, IStackChild
    where TRepository : GenericStackChildRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions>
    where TCreateEvent : CreatedEvent<TDto>, new()
    where TUpdateEvent : UpdatedEvent<TDto>, new()
    where TDeleteEvent : DeletedEvent<TDto>, new()
    where TOptions : class, IEntitySettings
{
    protected GenericStackChildSecuredRepository(
        TRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader],
        StackRoles = [StackRole.Owner, StackRole.Contributor, StackRole.Reader]
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor]
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor]
    };

    public override bool CanCreate(Guid parentId, Guid organizationId)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => CanCreateInStack<UserOrganizationRoleAssignment, UserStackRoleAssignment>(
                organizationId, principalId, parentId),
            PrincipalDiscriminator.ServicePrincipal => CanCreateInStack<ServicePrincipalOrganizationRoleAssignment, ServicePrincipalStackRoleAssignment>(
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
            [],
            []);
    }

    public override IQueryable<TEntity> ReadQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            ReadPermissionMap.OrganizationRoles,
            ReadPermissionMap.StackRoles,
            [],
            []);
    }

    public override IQueryable<TEntity> UpdateQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            UpdatePermissionMap.OrganizationRoles,
            UpdatePermissionMap.StackRoles,
            [],
            []);
    }

    public override IQueryable<TEntity> DeleteQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            DeletePermissionMap.OrganizationRoles,
            DeletePermissionMap.StackRoles,
            [],
            []);
    }

    public override string GetParentEntityName()
    {
        return "Stack";
    }

    protected IQueryable<TEntity> RoleQueryDispatch(
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
        var entitiesFromStackRoles =
            from entity in Repository.DbContext.Set<TEntity>()
            join assignment in Repository.DbContext.Set<TStackRoleAssignment>()
                on new { StackId = entity.StackId, entity.OrganizationId } equals new { assignment.StackId, assignment.OrganizationId }
            where entity.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && stackRoles.Contains(assignment.RoleName)
            select entity;

        var entitiesFromOrganizationRoles =
            from entity in Repository.DbContext.Set<TEntity>()
            join assignment in Repository.DbContext.Set<TOrganizationRoleAssignment>()
                on entity.OrganizationId equals assignment.OrganizationId
            where entity.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && organizationRoles.Contains(assignment.RoleName)
            select entity;

        var entitiesFromNamespaceRoles =
            from entity in Repository.DbContext.Set<TEntity>()
            join stack in Repository.DbContext.Stacks
                on new { StackId = entity.StackId, entity.OrganizationId } equals new { StackId = stack.Id, stack.OrganizationId }
            join ns in Repository.DbContext.Namespaces
                on new { StackId = stack.Id, stack.OrganizationId } equals new { ns.StackId, ns.OrganizationId }
            join assignment in Repository.DbContext.Set<TNamespaceRoleAssignment>()
                on new { NamespaceId = ns.Id, ns.OrganizationId } equals new { assignment.NamespaceId, assignment.OrganizationId }
            where entity.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && namespaceRoles.Contains(assignment.RoleName)
            select entity;

        var entitiesFromModuleRoles =
            from entity in Repository.DbContext.Set<TEntity>()
            join stack in Repository.DbContext.Stacks
                on new { StackId = entity.StackId, entity.OrganizationId } equals new { StackId = stack.Id, stack.OrganizationId }
            join ns in Repository.DbContext.Namespaces
                on new { StackId = stack.Id, stack.OrganizationId } equals new { ns.StackId, ns.OrganizationId }
            join mod in Repository.DbContext.Modules
                on new { NamespaceId = ns.Id, ns.OrganizationId } equals new { mod.NamespaceId, mod.OrganizationId }
            join assignment in Repository.DbContext.Set<TModuleRoleAssignment>()
                on new { ModuleId = mod.Id, mod.OrganizationId } equals new { assignment.ModuleId, assignment.OrganizationId }
            where entity.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && moduleRoles.Contains(assignment.RoleName)
            select entity;

        // Group-based role assignments
        var entitiesFromGroupStackRoles = StackRolesFromGroupQuery<TGroupMember, TStackRoleAssignment>(
            organizationId, principalId, stackRoles);

        var entitiesFromGroupOrganizationRoles = OrganizationRolesFromGroupQuery<TGroupMember, TOrganizationRoleAssignment>(
            organizationId, principalId, organizationRoles);

        var entitiesFromGroupNamespaceRoles = NamespaceRolesFromGroupQuery<TGroupMember, TNamespaceRoleAssignment>(
            organizationId, principalId, namespaceRoles);

        var entitiesFromGroupModuleRoles = ModuleRolesFromGroupQuery<TGroupMember, TModuleRoleAssignment>(
            organizationId, principalId, moduleRoles);


#if DEBUG
        // Materialize queries for debugging inspection
        var debugStackRoles = entitiesFromStackRoles.ToList();
        var debugOrganizationRoles = entitiesFromOrganizationRoles.ToList();
        var debugNamespaceRoles = entitiesFromNamespaceRoles.ToList();
        var debugModuleRoles = entitiesFromModuleRoles.ToList();
        var debugGroupStackRoles = entitiesFromGroupStackRoles.ToList();
        var debugGroupOrganizationRoles = entitiesFromGroupOrganizationRoles.ToList();
        var debugGroupNamespaceRoles = entitiesFromGroupNamespaceRoles.ToList();
        var debugGroupModuleRoles = entitiesFromGroupModuleRoles.ToList();
#endif

        return entitiesFromStackRoles
            .Concat(entitiesFromOrganizationRoles)
            .Concat(entitiesFromNamespaceRoles)
            .Concat(entitiesFromModuleRoles)
            .Concat(entitiesFromGroupStackRoles)
            .Concat(entitiesFromGroupOrganizationRoles)
            .Concat(entitiesFromGroupNamespaceRoles)
            .Concat(entitiesFromGroupModuleRoles);
    }

    private IQueryable<TEntity> StackRolesFromGroupQuery<TGroupMember, TStackRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<StackRole> stackRoles)
        where TGroupMember : class, IGroupMember
        where TStackRoleAssignment : class, IStackRoleAssignment
    {
        return from entity in Repository.DbContext.Set<TEntity>()
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                on entity.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupStackRoleAssignments
                on new { StackId = entity.StackId, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.StackId, assignment.OrganizationId, assignment.PrincipalId }
            where entity.OrganizationId == organizationId
                  && groupMember.PrincipalId == principalId
                  && stackRoles.Contains(assignment.RoleName)
            select entity;
    }

    private IQueryable<TEntity> OrganizationRolesFromGroupQuery<TGroupMember, TOrganizationRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<OrganizationRole> organizationRoles)
        where TGroupMember : class, IGroupMember
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
    {
        return from entity in Repository.DbContext.Set<TEntity>()
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                on entity.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupOrganizationRoleAssignments
                on new { OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.OrganizationId, assignment.PrincipalId }
            where entity.OrganizationId == organizationId
                  && groupMember.PrincipalId == principalId
                  && organizationRoles.Contains(assignment.RoleName)
            select entity;
    }

    private IQueryable<TEntity> NamespaceRolesFromGroupQuery<TGroupMember, TNamespaceRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<NamespaceRole> namespaceRoles)
        where TGroupMember : class, IGroupMember
        where TNamespaceRoleAssignment : class, INamespaceRoleAssignment
    {
        return from entity in Repository.DbContext.Set<TEntity>()
            join stack in Repository.DbContext.Stacks
                on new { StackId = entity.StackId, entity.OrganizationId } equals new { StackId = stack.Id, stack.OrganizationId }
            join ns in Repository.DbContext.Namespaces
                on new { StackId = stack.Id, stack.OrganizationId } equals new { ns.StackId, ns.OrganizationId }
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                on entity.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupNamespaceRoleAssignments
                on new { NamespaceId = ns.Id, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.NamespaceId, assignment.OrganizationId, assignment.PrincipalId }
            where entity.OrganizationId == organizationId
                  && groupMember.PrincipalId == principalId
                  && namespaceRoles.Contains(assignment.RoleName)
            select entity;
    }

    private IQueryable<TEntity> ModuleRolesFromGroupQuery<TGroupMember, TModuleRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<ModuleRole> moduleRoles)
        where TGroupMember : class, IGroupMember
        where TModuleRoleAssignment : class, IModuleRoleAssignment
    {
        return from entity in Repository.DbContext.Set<TEntity>()
            join stack in Repository.DbContext.Stacks
                on new { StackId = entity.StackId, entity.OrganizationId } equals new { StackId = stack.Id, stack.OrganizationId }
            join ns in Repository.DbContext.Namespaces
                on new { StackId = stack.Id, stack.OrganizationId } equals new { ns.StackId, ns.OrganizationId }
            join mod in Repository.DbContext.Modules
                on new { NamespaceId = ns.Id, ns.OrganizationId } equals new { mod.NamespaceId, mod.OrganizationId }
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                on entity.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupModuleRoleAssignments
                on new { ModuleId = mod.Id, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.ModuleId, assignment.OrganizationId, assignment.PrincipalId }
            where entity.OrganizationId == organizationId
                  && groupMember.PrincipalId == principalId
                  && moduleRoles.Contains(assignment.RoleName)
            select entity;
    }

    protected bool CanCreateInStack<TOrganizationRoleAssignment, TStackRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        Guid stackId)
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
        where TStackRoleAssignment : class, IStackRoleAssignment
    {
        // Check direct organization role assignment
        var hasOrgPermission = Repository.DbContext.Set<TOrganizationRoleAssignment>()
            .Any(ra => ra.OrganizationId == organizationId
                       && ra.PrincipalId == principalId
                       && (ra.RoleName == OrganizationRole.Owner || ra.RoleName == OrganizationRole.Contributor));

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
                where assignment.RoleName == OrganizationRole.Owner || assignment.RoleName == OrganizationRole.Contributor
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
                where assignment.RoleName == OrganizationRole.Owner || assignment.RoleName == OrganizationRole.Contributor
                select assignment
            ).Any(),
            _ => false
        };

        if (hasOrgPermissionViaGroup)
            return true;

        // Check direct stack role assignment
        var hasStackPermission = Repository.DbContext.Set<TStackRoleAssignment>()
            .Any(ra => ra.StackId == stackId
                       && ra.OrganizationId == organizationId
                       && ra.PrincipalId == principalId
                       && (ra.RoleName == StackRole.Owner || ra.RoleName == StackRole.Contributor));

        if (hasStackPermission)
            return true;

        // Check group-based stack role assignment
        var hasStackPermissionViaGroup = PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => (
                from gum in Repository.DbContext.UserGroupMembers
                where gum.UserId == principalId && gum.OrganizationId == organizationId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gum.GroupId, RootOrganizationId = gum.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupStackRoleAssignments
                    on new { StackId = stackId, OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.StackId, assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where assignment.RoleName == StackRole.Owner || assignment.RoleName == StackRole.Contributor
                select assignment
            ).Any(),
            PrincipalDiscriminator.ServicePrincipal => (
                from gspm in Repository.DbContext.ServicePrincipalGroupMembers
                where gspm.ServicePrincipalId == principalId && gspm.OrganizationId == organizationId
                join rgm in Repository.DbContext.RecursiveGroupMembers
                    on new { RootGroupId = gspm.GroupId, RootOrganizationId = gspm.OrganizationId }
                    equals new { rgm.RootGroupId, rgm.RootOrganizationId }
                join assignment in Repository.DbContext.GroupStackRoleAssignments
                    on new { StackId = stackId, OrganizationId = rgm.OrganizationId, GroupId = rgm.GroupId }
                    equals new { assignment.StackId, assignment.OrganizationId, GroupId = assignment.PrincipalId }
                where assignment.RoleName == StackRole.Owner || assignment.RoleName == StackRole.Contributor
                select assignment
            ).Any(),
            _ => false
        };

        return hasStackPermissionViaGroup;
    }
}