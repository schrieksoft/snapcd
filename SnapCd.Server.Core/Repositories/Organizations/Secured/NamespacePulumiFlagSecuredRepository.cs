using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.NamespacePulumiFlags;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class NamespacePulumiFlagSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<NamespacePulumiFlagRepositorySettings> options)
{
    public NamespacePulumiFlagSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespacePulumiFlagSecuredRepository(
            new NamespacePulumiFlagRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class NamespacePulumiFlagSecuredRepository : GenericNamespaceChildSecuredRepository<
    NamespacePulumiFlag,
    NamespacePulumiFlagReadDto,
    NamespacePulumiFlagRepository,
    NamespacePulumiFlagCreatedEvent,
    NamespacePulumiFlagUpdatedEvent,
    NamespacePulumiFlagDeletedEvent,
    NamespacePulumiFlagRepositorySettings>
{
    public NamespacePulumiFlagSecuredRepository(
        NamespacePulumiFlagRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }
}
