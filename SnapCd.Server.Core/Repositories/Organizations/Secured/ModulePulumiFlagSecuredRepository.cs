using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.ModulePulumiFlags;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class ModulePulumiFlagSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<ModulePulumiFlagRepositorySettings> options)
{
    public ModulePulumiFlagSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModulePulumiFlagSecuredRepository(
            new ModulePulumiFlagRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class ModulePulumiFlagSecuredRepository : GenericModuleChildSecuredRepository<
    ModulePulumiFlag,
    ModulePulumiFlagReadDto,
    ModulePulumiFlagRepository,
    ModulePulumiFlagCreatedEvent,
    ModulePulumiFlagUpdatedEvent,
    ModulePulumiFlagDeletedEvent,
    ModulePulumiFlagRepositorySettings>
{
    public ModulePulumiFlagSecuredRepository(
        ModulePulumiFlagRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }
}
