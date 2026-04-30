using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.ModuleHooks;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class ModuleHookSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<ModuleHookRepositorySettings> options)
{
    public ModuleHookSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleHookSecuredRepository(
            new ModuleHookRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class ModuleHookSecuredRepository : GenericModuleChildSecuredRepository<
    ModuleHook,
    ModuleHookReadDto,
    ModuleHookRepository,
    ModuleHookCreatedEvent,
    ModuleHookUpdatedEvent,
    ModuleHookDeletedEvent,
    ModuleHookRepositorySettings>
{
    public ModuleHookSecuredRepository(
        ModuleHookRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }
}
