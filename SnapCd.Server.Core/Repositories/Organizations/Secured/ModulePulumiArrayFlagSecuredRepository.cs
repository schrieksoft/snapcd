using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.ModulePulumiArrayFlags;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class ModulePulumiArrayFlagSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<ModulePulumiArrayFlagRepositorySettings> options)
{
    public ModulePulumiArrayFlagSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModulePulumiArrayFlagSecuredRepository(
            new ModulePulumiArrayFlagRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class ModulePulumiArrayFlagSecuredRepository : GenericModuleChildSecuredRepository<
    ModulePulumiArrayFlag,
    ModulePulumiArrayFlagReadDto,
    ModulePulumiArrayFlagRepository,
    ModulePulumiArrayFlagCreatedEvent,
    ModulePulumiArrayFlagUpdatedEvent,
    ModulePulumiArrayFlagDeletedEvent,
    ModulePulumiArrayFlagRepositorySettings>
{
    public ModulePulumiArrayFlagSecuredRepository(
        ModulePulumiArrayFlagRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }
}
