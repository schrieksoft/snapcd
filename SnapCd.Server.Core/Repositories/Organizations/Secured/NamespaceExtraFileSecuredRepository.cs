using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.NamespaceExtraFiles;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class NamespaceExtraFileSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<NamespaceExtraFileRepositorySettings> options)
{
    public NamespaceExtraFileSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespaceExtraFileSecuredRepository(
            new NamespaceExtraFileRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class NamespaceExtraFileSecuredRepository : GenericNamespaceChildSecuredRepository<
    NamespaceExtraFile,
    NamespaceExtraFileReadDto,
    NamespaceExtraFileRepository,
    NamespaceExtraFileCreatedEvent,
    NamespaceExtraFileUpdatedEvent,
    NamespaceExtraFileDeletedEvent,
    NamespaceExtraFileRepositorySettings>
{
    public NamespaceExtraFileSecuredRepository(
        NamespaceExtraFileRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public async Task<NamespaceExtraFile> Get(Guid namespaceId, string fileName, Guid organizationId)
    {
        var entity = await Repository.Get(namespaceId, fileName, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to NamespaceExtraFile {entity.Id}");

        return entity;
    }
}