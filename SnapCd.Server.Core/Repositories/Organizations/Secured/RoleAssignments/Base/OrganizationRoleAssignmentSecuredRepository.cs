using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.RoleAssignments.Base;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org.Base;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RoleAssignments.Base;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.RoleAssignments.Base;

public class OrganizationRoleAssignmentSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<OrganizationRoleAssignmentRepositorySettings> options)
{
    public OrganizationRoleAssignmentSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new OrganizationRoleAssignmentSecuredRepository(
            new OrganizationRoleAssignmentRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class OrganizationRoleAssignmentSecuredRepository : GenericOrganizationChildSecuredRepository<
    OrganizationRoleAssignment,
    OrganizationRoleAssignmentReadDto,
    OrganizationRoleAssignmentRepository,
    OrganizationRoleAssignmentCreatedEvent,
    OrganizationRoleAssignmentUpdatedEvent,
    OrganizationRoleAssignmentDeletedEvent,
    OrganizationRoleAssignmentRepositorySettings>
{
    public OrganizationRoleAssignmentSecuredRepository(
        OrganizationRoleAssignmentRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public override PermissionMap ReadPermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager]
    };

    public override PermissionMap UpdatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager]
    };

    public override PermissionMap CreatePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager]
    };

    public override PermissionMap DeletePermissionMap => new()
    {
        OrganizationRoles = [OrganizationRole.Owner, OrganizationRole.IdentityAccessManager]
    };

    public override async Task<OrganizationRoleAssignment> Create(OrganizationRoleAssignment entity, bool inTransaction = true)
    {
        throw new NotImplementedByDesignException(
            "OrganizationRoleAssignmentSecuredRepository can only be used for Get, List and Delete requests. For all others, use a repository for a concrete class.");
    }

    public override async Task<OrganizationRoleAssignment> Update(OrganizationRoleAssignment entity, bool inTransaction = true)
    {
        throw new NotImplementedByDesignException(
            "OrganizationRoleAssignmentSecuredRepository can only be used for Get, List and Delete requests. For all others, use a repository for a concrete class.");
    }
}