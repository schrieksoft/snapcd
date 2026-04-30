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

public class UserStackRoleAssignmentSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<UserStackRoleAssignmentRepositorySettings> options)
{
    public UserStackRoleAssignmentSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new UserStackRoleAssignmentSecuredRepository(
            new UserStackRoleAssignmentRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class UserStackRoleAssignmentSecuredRepository : GenericStackChildSecuredRepository<
    UserStackRoleAssignment,
    UserStackRoleAssignmentReadDto,
    UserStackRoleAssignmentRepository,
    UserStackRoleAssignmentCreatedEvent,
    UserStackRoleAssignmentUpdatedEvent,
    UserStackRoleAssignmentDeletedEvent,
    UserStackRoleAssignmentRepositorySettings>
{
    public UserStackRoleAssignmentSecuredRepository(
        UserStackRoleAssignmentRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager],
        StackRoles = [StackRole.Owner, StackRole.IdentityAccessManager]
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager],
        StackRoles = [StackRole.Owner, StackRole.IdentityAccessManager]
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager],
        StackRoles = [StackRole.Owner, StackRole.IdentityAccessManager]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager],
        StackRoles = [StackRole.Owner, StackRole.IdentityAccessManager]
    };

    public async Task<List<UserStackRoleAssignment>> ListByUser(Guid userId, Guid organizationId)
    {
        return await Repository.ListByUser(userId, organizationId);
    }

    public async Task<List<UserStackRoleAssignment>> ListByStack(Guid stackId, Guid organizationId)
    {
        return await Repository.ListByStack(stackId, organizationId);
    }

    public async Task<List<UserStackRoleAssignment>> ListByRole(StackRole role, Guid organizationId)
    {
        return await Repository.ListByRole(role, organizationId);
    }
}