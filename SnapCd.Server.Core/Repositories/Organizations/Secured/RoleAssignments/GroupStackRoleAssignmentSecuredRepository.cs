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

public class GroupStackRoleAssignmentSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<GroupStackRoleAssignmentRepositorySettings> options)
{
    public GroupStackRoleAssignmentSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new GroupStackRoleAssignmentSecuredRepository(
            new GroupStackRoleAssignmentRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class GroupStackRoleAssignmentSecuredRepository : GenericStackChildSecuredRepository<
    GroupStackRoleAssignment,
    GroupStackRoleAssignmentReadDto,
    GroupStackRoleAssignmentRepository,
    GroupStackRoleAssignmentCreatedEvent,
    GroupStackRoleAssignmentUpdatedEvent,
    GroupStackRoleAssignmentDeletedEvent,
    GroupStackRoleAssignmentRepositorySettings>
{
    public GroupStackRoleAssignmentSecuredRepository(
        GroupStackRoleAssignmentRepository repository,
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

    public async Task<List<GroupStackRoleAssignment>> ListByGroup(Guid groupId, Guid organizationId)
    {
        return await Repository.ListByGroup(groupId, organizationId);
    }

    public async Task<List<GroupStackRoleAssignment>> ListByStack(Guid stackId, Guid organizationId)
    {
        return await Repository.ListByStack(stackId, organizationId);
    }

    public async Task<List<GroupStackRoleAssignment>> ListByRole(StackRole role, Guid organizationId)
    {
        return await Repository.ListByRole(role, organizationId);
    }
}