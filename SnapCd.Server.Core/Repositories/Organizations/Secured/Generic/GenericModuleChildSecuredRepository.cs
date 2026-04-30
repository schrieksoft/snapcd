using SnapCd.Contracts;
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

public abstract class GenericModuleChildSecuredRepository<TEntity, TDto, TRepository, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions> :
    GenericSecuredRepository<TEntity, TDto, TRepository, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions>
    where TEntity : class, IEntity, IModuleChild
    where TRepository : GenericModuleChildRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions>
    where TCreateEvent : CreatedEvent<TDto>, new()
    where TUpdateEvent : UpdatedEvent<TDto>, new()
    where TDeleteEvent : DeletedEvent<TDto>, new()
    where TOptions : class, IEntitySettings
{
    protected GenericModuleChildSecuredRepository(
        TRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader],
        StackRoles = [StackRole.Owner, StackRole.Contributor, StackRole.Reader],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor, NamespaceRole.Reader],
        ModuleRoles = [ModuleRole.Owner, ModuleRole.Reader]
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor],
        ModuleRoles = [ModuleRole.Owner]
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor],
        ModuleRoles = [ModuleRole.Owner]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.Contributor],
        StackRoles = [StackRole.Owner, StackRole.Contributor],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.Contributor],
        ModuleRoles = [ModuleRole.Owner]
    };

    public override bool CanCreate(Guid parentId, Guid organizationId)
    {
        var principalId = PrincipalProvider.GetSubject(organizationId);

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => CanCreateInModule<
                UserOrganizationRoleAssignment,
                UserStackRoleAssignment,
                UserNamespaceRoleAssignment,
                UserModuleRoleAssignment,
                UserGroupMember>(
                organizationId, principalId, parentId),
            PrincipalDiscriminator.ServicePrincipal => CanCreateInModule<
                ServicePrincipalOrganizationRoleAssignment,
                ServicePrincipalStackRoleAssignment,
                ServicePrincipalNamespaceRoleAssignment,
                ServicePrincipalModuleRoleAssignment,
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
            CreatePermissionMap.ModuleRoles);
    }

    public override IQueryable<TEntity> ReadQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            ReadPermissionMap.OrganizationRoles,
            ReadPermissionMap.StackRoles,
            ReadPermissionMap.NamespaceRoles,
            ReadPermissionMap.ModuleRoles);
    }

    public override IQueryable<TEntity> UpdateQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            UpdatePermissionMap.OrganizationRoles,
            UpdatePermissionMap.StackRoles,
            UpdatePermissionMap.NamespaceRoles,
            UpdatePermissionMap.ModuleRoles);
    }

    public override IQueryable<TEntity> DeleteQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            DeletePermissionMap.OrganizationRoles,
            DeletePermissionMap.StackRoles,
            DeletePermissionMap.NamespaceRoles,
            DeletePermissionMap.ModuleRoles);
    }

    public override string GetParentEntityName()
    {
        return "Module";
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
        var entitiesFromModuleRoles =
            from entity in Repository.DbContext.Set<TEntity>()
            join assignment in Repository.DbContext.Set<TModuleRoleAssignment>()
                on new { ModuleId = entity.ModuleId, entity.OrganizationId } equals new { assignment.ModuleId, assignment.OrganizationId }
            where entity.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && moduleRoles.Contains(assignment.RoleName)
            select entity;

        var entitiesFromNamespaceRoles =
            from entity in Repository.DbContext.Set<TEntity>()
            join module in Repository.DbContext.Modules
                on new { ModuleId = entity.ModuleId, entity.OrganizationId } equals new { ModuleId = module.Id, module.OrganizationId }
            join assignment in Repository.DbContext.Set<TNamespaceRoleAssignment>()
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { assignment.NamespaceId, assignment.OrganizationId }
            where entity.OrganizationId == organizationId
                  && assignment.PrincipalId == principalId
                  && namespaceRoles.Contains(assignment.RoleName)
            select entity;

        var entitiesFromStackRoles =
            from entity in Repository.DbContext.Set<TEntity>()
            join module in Repository.DbContext.Modules
                on new { ModuleId = entity.ModuleId, entity.OrganizationId } equals new { ModuleId = module.Id, module.OrganizationId }
            join ns in Repository.DbContext.Namespaces
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
            join stack in Repository.DbContext.Stacks
                on new { StackId = ns.StackId, ns.OrganizationId } equals new { StackId = stack.Id, stack.OrganizationId }
            join assignment in Repository.DbContext.Set<TStackRoleAssignment>()
                on new { StackId = stack.Id, stack.OrganizationId } equals new { assignment.StackId, assignment.OrganizationId }
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

        // Group-based role assignments (via UserGroupMember)
        var entitiesFromGroupUserModuleRoles = ModuleRolesFromGroupQuery<TGroupMember, TModuleRoleAssignment>(
            organizationId, principalId, moduleRoles);

        var entitiesFromGroupUserNamespaceRoles = NamespaceRolesFromGroupQuery<TGroupMember, TNamespaceRoleAssignment>(
            organizationId, principalId, namespaceRoles);

        var entitiesFromGroupUserStackRoles = StackRolesFromGroupQuery<TGroupMember, TStackRoleAssignment>(
            organizationId, principalId, stackRoles);

        var entitiesFromGroupUserOrganizationRoles = OrganizationRolesFromGroupQuery<TGroupMember, TOrganizationRoleAssignment>(
            organizationId, principalId, organizationRoles);


        // Concat all queries
        return entitiesFromModuleRoles
            .Concat(entitiesFromNamespaceRoles)
            .Concat(entitiesFromStackRoles)
            .Concat(entitiesFromOrganizationRoles)
            .Concat(entitiesFromGroupUserModuleRoles)
            .Concat(entitiesFromGroupUserNamespaceRoles)
            .Concat(entitiesFromGroupUserStackRoles)
            .Concat(entitiesFromGroupUserOrganizationRoles);
    }

    private IQueryable<TEntity> ModuleRolesFromGroupQuery<TGroupMember, TModuleRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<ModuleRole> moduleRoles)
        where TGroupMember : class, IGroupMember
        where TModuleRoleAssignment : class, IModuleRoleAssignment
    {
        return from entity in Repository.DbContext.Set<TEntity>()
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                on entity.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupModuleRoleAssignments
                on new { ModuleId = entity.ModuleId, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.ModuleId, assignment.OrganizationId, assignment.PrincipalId }
            where entity.OrganizationId == organizationId
                  && groupMember.PrincipalId == principalId
                  && moduleRoles.Contains(assignment.RoleName)
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
            join module in Repository.DbContext.Modules
                on new { ModuleId = entity.ModuleId, entity.OrganizationId } equals new { ModuleId = module.Id, module.OrganizationId }
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                on entity.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupNamespaceRoleAssignments
                on new { NamespaceId = module.NamespaceId, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.NamespaceId, assignment.OrganizationId, assignment.PrincipalId }
            where entity.OrganizationId == organizationId
                  && groupMember.PrincipalId == principalId
                  && namespaceRoles.Contains(assignment.RoleName)
            select entity;
    }

    private IQueryable<TEntity> StackRolesFromGroupQuery<TGroupMember, TStackRoleAssignment>(
        Guid organizationId,
        Guid principalId,
        List<StackRole> stackRoles)
        where TGroupMember : class, IGroupMember
        where TStackRoleAssignment : class, IStackRoleAssignment
    {
        return from entity in Repository.DbContext.Set<TEntity>()
            join module in Repository.DbContext.Modules
                on new { ModuleId = entity.ModuleId, entity.OrganizationId } equals new { ModuleId = module.Id, module.OrganizationId }
            join ns in Repository.DbContext.Namespaces
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
            join stack in Repository.DbContext.Stacks
                on new { StackId = ns.StackId, ns.OrganizationId } equals new { StackId = stack.Id, stack.OrganizationId }
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                on entity.OrganizationId equals groupMember.OrganizationId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupStackRoleAssignments
                on new { StackId = stack.Id, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
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

    protected IQueryable<Module> ModuleRoleQuery<TOrganizationRoleAssignment, TStackRoleAssignment, TNamespaceRoleAssignment, TModuleRoleAssignment, TGroupMember>(
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
        // Direct module role assignment
        var modulesFromModuleRoles =
            from module in Repository.DbContext.Modules
            where module.OrganizationId == organizationId
            join assignment in Repository.DbContext.Set<TModuleRoleAssignment>()
                on new { ModuleId = module.Id, module.OrganizationId } equals new { assignment.ModuleId, assignment.OrganizationId }
            where assignment.PrincipalId == principalId
                  && moduleRoles.Contains(assignment.RoleName)
            select module;

        // Direct namespace role assignment
        var modulesFromNamespaceRoles =
            from module in Repository.DbContext.Modules
            where module.OrganizationId == organizationId
            join assignment in Repository.DbContext.Set<TNamespaceRoleAssignment>()
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { assignment.NamespaceId, assignment.OrganizationId }
            where assignment.PrincipalId == principalId
                  && namespaceRoles.Contains(assignment.RoleName)
            select module;

        // Direct stack role assignment
        var modulesFromStackRoles =
            from module in Repository.DbContext.Modules
            where module.OrganizationId == organizationId
            join ns in Repository.DbContext.Namespaces
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
            join assignment in Repository.DbContext.Set<TStackRoleAssignment>()
                on new { StackId = ns.StackId, ns.OrganizationId } equals new { assignment.StackId, assignment.OrganizationId }
            where assignment.PrincipalId == principalId
                  && stackRoles.Contains(assignment.RoleName)
            select module;

        // Direct organization role assignment
        var modulesFromOrganizationRoles =
            from module in Repository.DbContext.Modules
            where module.OrganizationId == organizationId
            join assignment in Repository.DbContext.Set<TOrganizationRoleAssignment>()
                on module.OrganizationId equals assignment.OrganizationId
            where assignment.PrincipalId == principalId
                  && organizationRoles.Contains(assignment.RoleName)
            select module;

        // Group-based module role assignment
        var modulesFromGroupModuleRoles =
            from module in Repository.DbContext.Modules
            where module.OrganizationId == organizationId
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                on module.OrganizationId equals groupMember.OrganizationId
            where groupMember.PrincipalId == principalId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupModuleRoleAssignments
                on new { ModuleId = module.Id, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.ModuleId, assignment.OrganizationId, assignment.PrincipalId }
            where moduleRoles.Contains(assignment.RoleName)
            select module;

        // Group-based namespace role assignment
        var modulesFromGroupNamespaceRoles =
            from module in Repository.DbContext.Modules
            where module.OrganizationId == organizationId
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                on module.OrganizationId equals groupMember.OrganizationId
            where groupMember.PrincipalId == principalId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupNamespaceRoleAssignments
                on new { NamespaceId = module.NamespaceId, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.NamespaceId, assignment.OrganizationId, assignment.PrincipalId }
            where namespaceRoles.Contains(assignment.RoleName)
            select module;

        // Group-based stack role assignment
        var modulesFromGroupStackRoles =
            from module in Repository.DbContext.Modules
            where module.OrganizationId == organizationId
            join ns in Repository.DbContext.Namespaces
                on new { NamespaceId = module.NamespaceId, module.OrganizationId } equals new { NamespaceId = ns.Id, ns.OrganizationId }
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                on module.OrganizationId equals groupMember.OrganizationId
            where groupMember.PrincipalId == principalId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupStackRoleAssignments
                on new { StackId = ns.StackId, OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.StackId, assignment.OrganizationId, assignment.PrincipalId }
            where stackRoles.Contains(assignment.RoleName)
            select module;

        // Group-based organization role assignment
        var modulesFromGroupOrganizationRoles =
            from module in Repository.DbContext.Modules
            where module.OrganizationId == organizationId
            join groupMember in Repository.DbContext.Set<TGroupMember>()
                on module.OrganizationId equals groupMember.OrganizationId
            where groupMember.PrincipalId == principalId
            join rgm in Repository.DbContext.RecursiveGroupMembers
                on new { RootGroupId = groupMember.GroupId, RootOrganizationId = groupMember.OrganizationId }
                equals new { rgm.RootGroupId, rgm.RootOrganizationId }
            join assignment in Repository.DbContext.GroupOrganizationRoleAssignments
                on new { OrganizationId = rgm.OrganizationId, PrincipalId = rgm.GroupId }
                equals new { assignment.OrganizationId, assignment.PrincipalId }
            where organizationRoles.Contains(assignment.RoleName)
            select module;

        // Concat all queries
        return modulesFromModuleRoles
            .Concat(modulesFromNamespaceRoles)
            .Concat(modulesFromStackRoles)
            .Concat(modulesFromOrganizationRoles)
            .Concat(modulesFromGroupModuleRoles)
            .Concat(modulesFromGroupNamespaceRoles)
            .Concat(modulesFromGroupStackRoles)
            .Concat(modulesFromGroupOrganizationRoles);
    }

    protected bool CanCreateInModule<TOrganizationRoleAssignment, TStackRoleAssignment, TNamespaceRoleAssignment, TModuleRoleAssignment, TGroupMember>(
        Guid organizationId,
        Guid principalId,
        Guid moduleId)
        where TOrganizationRoleAssignment : class, IOrganizationRoleAssignment
        where TStackRoleAssignment : class, IStackRoleAssignment
        where TNamespaceRoleAssignment : class, INamespaceRoleAssignment
        where TModuleRoleAssignment : class, IModuleRoleAssignment
        where TGroupMember : class, IGroupMember
    {
        return ModuleRoleQuery<TOrganizationRoleAssignment, TStackRoleAssignment, TNamespaceRoleAssignment, TModuleRoleAssignment, TGroupMember>(
            organizationId,
            principalId,
            CreatePermissionMap.OrganizationRoles,
            CreatePermissionMap.StackRoles,
            CreatePermissionMap.NamespaceRoles,
            CreatePermissionMap.ModuleRoles).Any(m => m.Id == moduleId && m.OrganizationId == organizationId);
    }
}