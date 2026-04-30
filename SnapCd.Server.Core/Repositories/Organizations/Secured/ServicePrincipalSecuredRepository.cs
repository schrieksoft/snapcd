using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.ServicePrincipals;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class ServicePrincipalSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<ServicePrincipalRepositorySettings> options)
{
    public ServicePrincipalSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ServicePrincipalSecuredRepository(
            new ServicePrincipalRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class ServicePrincipalSecuredRepository : GenericOrganizationChildSecuredRepository<
    ServicePrincipal,
    ServicePrincipalReadDto,
    ServicePrincipalRepository,
    ServicePrincipalCreatedEvent,
    ServicePrincipalUpdatedEvent,
    ServicePrincipalDeletedEvent,
    ServicePrincipalRepositorySettings>
{
    public ServicePrincipalSecuredRepository(
        ServicePrincipalRepository repository,
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

    public async Task<ServicePrincipal?> GetByClientId(string clientId, Guid organizationId)
    {
        var entity = await Repository.GetByClientId(clientId, organizationId);
        
        if (entity == null)
            throw new EntityNotFoundException($"Unable to find ServicePrincipal with ClientId \"{clientId}\"");
        
        if (!CanRead(entity.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to ServicePrincipal {entity.Id}");

        return entity;
        
    }
}