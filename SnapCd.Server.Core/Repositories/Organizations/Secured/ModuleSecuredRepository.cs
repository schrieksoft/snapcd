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
            [OrganizationRole.Owner, OrganizationRole.Contributor],
            [StackRole.Owner, StackRole.Contributor],
            [NamespaceRole.Owner, NamespaceRole.Contributor, NamespaceRole.ModuleCreator],
            []
        );
    }

    public override IQueryable<Module> ReadQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            [OrganizationRole.Owner, OrganizationRole.Contributor, OrganizationRole.Reader],
            [StackRole.Owner, StackRole.Contributor, StackRole.Reader],
            [NamespaceRole.Owner, NamespaceRole.Contributor, NamespaceRole.Reader],
            [ModuleRole.Owner, ModuleRole.Reader]
        );
    }

    public override IQueryable<Module> UpdateQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            [OrganizationRole.Owner, OrganizationRole.Contributor],
            [StackRole.Owner, StackRole.Contributor],
            [NamespaceRole.Owner, NamespaceRole.Contributor],
            [ModuleRole.Owner]
        );
    }

    public override IQueryable<Module> DeleteQuery(Guid organizationId)
    {
        return RoleQueryDispatch(
            organizationId,
            [OrganizationRole.Owner, OrganizationRole.Contributor],
            [StackRole.Owner, StackRole.Contributor],
            [NamespaceRole.Owner, NamespaceRole.Contributor],
            [ModuleRole.Owner]
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
        var hasStackPermission = (
            from ns in Repository.DbContext.Namespaces
            where ns.Id == namespaceId && ns.OrganizationId == organizationId
            join stack in Repository.DbContext.Stacks
                on new { StackId = ns.StackId, ns.OrganizationId } equals new { StackId = stack.Id, stack.OrganizationId }
            join assignment in Repository.DbContext.Set<TStackRoleAssignment>()
                on new { StackId = stack.Id, stack.OrganizationId } equals new { assignment.StackId, assignment.OrganizationId }
            where assignment.PrincipalId == principalId
                  && (assignment.RoleName == StackRole.Owner || assignment.RoleName == StackRole.Contributor)
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
                      && (assignment.RoleName == StackRole.Owner || assignment.RoleName == StackRole.Contributor)
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
                      && (assignment.RoleName == StackRole.Owner || assignment.RoleName == StackRole.Contributor)
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
                       && (ra.RoleName == NamespaceRole.Owner
                           || ra.RoleName == NamespaceRole.Contributor
                           || ra.RoleName == NamespaceRole.ModuleCreator));

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
                where assignment.RoleName == NamespaceRole.Owner
                      || assignment.RoleName == NamespaceRole.Contributor
                      || assignment.RoleName == NamespaceRole.ModuleCreator
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
                where assignment.RoleName == NamespaceRole.Owner
                      || assignment.RoleName == NamespaceRole.Contributor
                      || assignment.RoleName == NamespaceRole.ModuleCreator
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