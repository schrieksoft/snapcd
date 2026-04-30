using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.NamespaceBackendConfigs;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class NamespaceBackendConfigSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<NamespaceBackendConfigRepositorySettings> options)
{
    public NamespaceBackendConfigSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespaceBackendConfigSecuredRepository(
            new NamespaceBackendConfigRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class NamespaceBackendConfigSecuredRepository : GenericNamespaceChildSecuredRepository<
    NamespaceBackendConfig,
    NamespaceBackendConfigReadDto,
    NamespaceBackendConfigRepository,
    NamespaceBackendConfigCreatedEvent,
    NamespaceBackendConfigUpdatedEvent,
    NamespaceBackendConfigDeletedEvent,
    NamespaceBackendConfigRepositorySettings>
{
    public NamespaceBackendConfigSecuredRepository(
        NamespaceBackendConfigRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }


    public async Task<NamespaceBackendConfig> Get(Guid namespaceId, string name, Guid organizationId)
    {
        var entity = await Repository.Get(namespaceId, name, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to NamespaceBackendConfig {entity.Id}");

        return entity;
    }
}