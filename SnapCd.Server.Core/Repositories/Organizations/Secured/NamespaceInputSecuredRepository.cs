using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.NamespaceInputs.Base;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class NamespaceInputSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<NamespaceInputRepositorySettings> options)
{
    public NamespaceInputSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespaceInputSecuredRepository(
            new NamespaceInputRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class NamespaceInputSecuredRepository : GenericNamespaceChildSecuredRepository<
    NamespaceInput,
    NamespaceInputReadDto,
    NamespaceInputRepository,
    NamespaceInputCreatedEvent,
    NamespaceInputUpdatedEvent,
    NamespaceInputDeletedEvent,
    NamespaceInputRepositorySettings>
{
    public NamespaceInputSecuredRepository(
        NamespaceInputRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public async Task<NamespaceInput> Get(Guid namespaceId, string name, Guid organizationId)
    {
        var entity = await Repository.Get(namespaceId, name, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to {nameof(NamespaceInput)} {entity.Id}");

        return entity;
    }
}