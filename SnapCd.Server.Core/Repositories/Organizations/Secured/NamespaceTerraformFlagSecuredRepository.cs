using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.NamespaceTerraformFlags;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class NamespaceTerraformFlagSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<NamespaceTerraformFlagRepositorySettings> options)
{
    public NamespaceTerraformFlagSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespaceTerraformFlagSecuredRepository(
            new NamespaceTerraformFlagRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class NamespaceTerraformFlagSecuredRepository : GenericNamespaceChildSecuredRepository<
    NamespaceTerraformFlag,
    NamespaceTerraformFlagReadDto,
    NamespaceTerraformFlagRepository,
    NamespaceTerraformFlagCreatedEvent,
    NamespaceTerraformFlagUpdatedEvent,
    NamespaceTerraformFlagDeletedEvent,
    NamespaceTerraformFlagRepositorySettings>
{
    public NamespaceTerraformFlagSecuredRepository(
        NamespaceTerraformFlagRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }
}
