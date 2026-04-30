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

public class UserNamespaceRoleAssignmentSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<UserNamespaceRoleAssignmentRepositorySettings> options)
{
    public UserNamespaceRoleAssignmentSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new UserNamespaceRoleAssignmentSecuredRepository(
            new UserNamespaceRoleAssignmentRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class UserNamespaceRoleAssignmentSecuredRepository : GenericNamespaceChildSecuredRepository<
    UserNamespaceRoleAssignment,
    UserNamespaceRoleAssignmentReadDto,
    UserNamespaceRoleAssignmentRepository,
    UserNamespaceRoleAssignmentCreatedEvent,
    UserNamespaceRoleAssignmentUpdatedEvent,
    UserNamespaceRoleAssignmentDeletedEvent,
    UserNamespaceRoleAssignmentRepositorySettings>
{
    public UserNamespaceRoleAssignmentSecuredRepository(
        UserNamespaceRoleAssignmentRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager],
        StackRoles = [StackRole.Owner, StackRole.IdentityAccessManager],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.IdentityAccessManager]
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager],
        StackRoles = [StackRole.Owner, StackRole.IdentityAccessManager],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.IdentityAccessManager]
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager],
        StackRoles = [StackRole.Owner, StackRole.IdentityAccessManager],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.IdentityAccessManager]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager],
        StackRoles = [StackRole.Owner, StackRole.IdentityAccessManager],
        NamespaceRoles = [NamespaceRole.Owner, NamespaceRole.IdentityAccessManager]
    };

    public async Task<List<UserNamespaceRoleAssignment>> ListByUser(Guid userId, Guid organizationId)
    {
        return await Repository.ListByUser(userId, organizationId);
    }

    public async Task<List<UserNamespaceRoleAssignment>> ListByNamespace(Guid namespaceId, Guid organizationId)
    {
        return await Repository.ListByNamespace(namespaceId, organizationId);
    }

    public async Task<List<UserNamespaceRoleAssignment>> ListByRole(NamespaceRole role, Guid organizationId)
    {
        return await Repository.ListByRole(role, organizationId);
    }
}