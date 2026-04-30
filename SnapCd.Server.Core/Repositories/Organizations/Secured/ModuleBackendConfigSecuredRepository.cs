using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.ModuleBackendConfigs;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class ModuleBackendConfigSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<ModuleBackendConfigRepositorySettings> options)
{
    public ModuleBackendConfigSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleBackendConfigSecuredRepository(
            new ModuleBackendConfigRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class ModuleBackendConfigSecuredRepository : GenericModuleChildSecuredRepository<
    ModuleBackendConfig,
    ModuleBackendConfigReadDto,
    ModuleBackendConfigRepository,
    ModuleBackendConfigCreatedEvent,
    ModuleBackendConfigUpdatedEvent,
    ModuleBackendConfigDeletedEvent,
    ModuleBackendConfigRepositorySettings>
{
    public ModuleBackendConfigSecuredRepository(
        ModuleBackendConfigRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public async Task<ModuleBackendConfig> Get(Guid moduleId, string name, Guid organizationId)
    {
        var entity = await Repository.Get(moduleId, name, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to ModuleBackendConfig {entity.Id}");

        return entity;
    }
}