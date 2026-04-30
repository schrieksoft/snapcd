using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.NamespacePulumiArrayFlags;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class NamespacePulumiArrayFlagSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<NamespacePulumiArrayFlagRepositorySettings> options)
{
    public NamespacePulumiArrayFlagSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespacePulumiArrayFlagSecuredRepository(
            new NamespacePulumiArrayFlagRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class NamespacePulumiArrayFlagSecuredRepository : GenericNamespaceChildSecuredRepository<
    NamespacePulumiArrayFlag,
    NamespacePulumiArrayFlagReadDto,
    NamespacePulumiArrayFlagRepository,
    NamespacePulumiArrayFlagCreatedEvent,
    NamespacePulumiArrayFlagUpdatedEvent,
    NamespacePulumiArrayFlagDeletedEvent,
    NamespacePulumiArrayFlagRepositorySettings>
{
    public NamespacePulumiArrayFlagSecuredRepository(
        NamespacePulumiArrayFlagRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }
}
