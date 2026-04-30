using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.NamespaceHooks;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class NamespaceHookSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<NamespaceHookRepositorySettings> options)
{
    public NamespaceHookSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespaceHookSecuredRepository(
            new NamespaceHookRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class NamespaceHookSecuredRepository : GenericNamespaceChildSecuredRepository<
    NamespaceHook,
    NamespaceHookReadDto,
    NamespaceHookRepository,
    NamespaceHookCreatedEvent,
    NamespaceHookUpdatedEvent,
    NamespaceHookDeletedEvent,
    NamespaceHookRepositorySettings>
{
    public NamespaceHookSecuredRepository(
        NamespaceHookRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }
}
