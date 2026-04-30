using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.NamespaceInputs;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class NamespaceInputFromDefinitionSecuredRepositoryFactory<TEntity>(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<NamespaceInputFromDefinitionRepositorySettings> options)
    where TEntity : NamespaceInput, INamespaceInputFromDefinition
{
    public NamespaceInputFromDefinitionSecuredRepository<TEntity> Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespaceInputFromDefinitionSecuredRepository<TEntity>(
            new NamespaceInputFromDefinitionRepository<TEntity>(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class NamespaceInputFromDefinitionSecuredRepository<TEntity> : GenericNamespaceChildSecuredRepository<
    TEntity,
    NamespaceInputFromDefinitionReadDto,
    NamespaceInputFromDefinitionRepository<TEntity>,
    NamespaceInputFromDefinitionCreatedEvent,
    NamespaceInputFromDefinitionUpdatedEvent,
    NamespaceInputFromDefinitionDeletedEvent,
    NamespaceInputFromDefinitionRepositorySettings>
    where TEntity : NamespaceInput, INamespaceInputFromDefinition
{
    public NamespaceInputFromDefinitionSecuredRepository(
        NamespaceInputFromDefinitionRepository<TEntity> repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public async Task<TEntity> Get(Guid namespaceId, string name, Guid organizationId)
    {
        var entity = await Repository.Get(namespaceId, name, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to {typeof(TEntity).Name} {entity.Id}");

        return entity;
    }
}