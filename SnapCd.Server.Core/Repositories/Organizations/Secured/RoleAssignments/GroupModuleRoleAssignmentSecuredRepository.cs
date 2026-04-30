using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RoleAssignments;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments;

public class GroupModuleRoleAssignmentSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<GroupModuleRoleAssignmentRepositorySettings> options)
{
    public GroupModuleRoleAssignmentSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new GroupModuleRoleAssignmentSecuredRepository(
            new GroupModuleRoleAssignmentRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class GroupModuleRoleAssignmentSecuredRepository : GenericModuleChildSecuredRepository<
    GroupModuleRoleAssignment,
    GroupModuleRoleAssignmentReadDto,
    GroupModuleRoleAssignmentRepository,
    GroupModuleRoleAssignmentCreatedEvent,
    GroupModuleRoleAssignmentUpdatedEvent,
    GroupModuleRoleAssignmentDeletedEvent,
    GroupModuleRoleAssignmentRepositorySettings>
{
    public GroupModuleRoleAssignmentSecuredRepository(
        GroupModuleRoleAssignmentRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager],
        StackRoles = [StackRole.Owner, StackRole.IdentityAccessManager],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.IdentityAccessManager],
        ModuleRoles = [ModuleRole.Owner, ModuleRole.IdentityAccessManager]
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager],
        StackRoles = [StackRole.Owner, StackRole.IdentityAccessManager],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.IdentityAccessManager],
        ModuleRoles = [ModuleRole.Owner, ModuleRole.IdentityAccessManager]
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager],
        StackRoles = [StackRole.Owner, StackRole.IdentityAccessManager],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.IdentityAccessManager],
        ModuleRoles = [ModuleRole.Owner, ModuleRole.IdentityAccessManager]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager],
        StackRoles = [StackRole.Owner, StackRole.IdentityAccessManager],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.IdentityAccessManager],
        ModuleRoles = [ModuleRole.Owner, ModuleRole.IdentityAccessManager]
    };

    public async Task<List<GroupModuleRoleAssignment>> ListByGroup(Guid groupId, Guid organizationId)
    {
        return await Repository.ListByGroup(groupId, organizationId);
    }

    public async Task<List<GroupModuleRoleAssignment>> ListByModule(Guid moduleId, Guid organizationId)
    {
        return await Repository.ListByModule(moduleId, organizationId);
    }

    public async Task<List<GroupModuleRoleAssignment>> ListByRole(ModuleRole role, Guid organizationId)
    {
        return await Repository.ListByRole(role, organizationId);
    }
}