using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.NamespaceTerraformArrayFlags;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class NamespaceTerraformArrayFlagSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<NamespaceTerraformArrayFlagRepositorySettings> options)
{
    public NamespaceTerraformArrayFlagSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespaceTerraformArrayFlagSecuredRepository(
            new NamespaceTerraformArrayFlagRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class NamespaceTerraformArrayFlagSecuredRepository : GenericNamespaceChildSecuredRepository<
    NamespaceTerraformArrayFlag,
    NamespaceTerraformArrayFlagReadDto,
    NamespaceTerraformArrayFlagRepository,
    NamespaceTerraformArrayFlagCreatedEvent,
    NamespaceTerraformArrayFlagUpdatedEvent,
    NamespaceTerraformArrayFlagDeletedEvent,
    NamespaceTerraformArrayFlagRepositorySettings>
{
    public NamespaceTerraformArrayFlagSecuredRepository(
        NamespaceTerraformArrayFlagRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }
}
