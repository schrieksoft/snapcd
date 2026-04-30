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

public class NamespaceInputFromLiteralSecuredRepositoryFactory<TEntity>(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<NamespaceInputFromLiteralRepositorySettings> options)
    where TEntity : NamespaceInputWithType, INamespaceInputFromLiteral
{
    public NamespaceInputFromLiteralSecuredRepository<TEntity> Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespaceInputFromLiteralSecuredRepository<TEntity>(
            new NamespaceInputFromLiteralRepository<TEntity>(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class NamespaceInputFromLiteralSecuredRepository<TEntity> : GenericNamespaceChildSecuredRepository<
    TEntity,
    NamespaceInputFromLiteralReadDto,
    NamespaceInputFromLiteralRepository<TEntity>,
    NamespaceInputFromLiteralCreatedEvent,
    NamespaceInputFromLiteralUpdatedEvent,
    NamespaceInputFromLiteralDeletedEvent,
    NamespaceInputFromLiteralRepositorySettings>
    where TEntity : NamespaceInputWithType, INamespaceInputFromLiteral
{
    public NamespaceInputFromLiteralSecuredRepository(
        NamespaceInputFromLiteralRepository<TEntity> repository,
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

    public async Task<Dictionary<string, TEntity>> GetLiterals(
        Guid namespaceId,
        List<string> envVarNames,
        Guid organizationId)
    {
        // Check if user can read from the namespace
        var namespaceEntity = await Repository.DbContext.Namespaces.FindAsync(namespaceId);
        if (namespaceEntity != null && !ReadQuery(organizationId).Any(e => e.NamespaceId == namespaceId))
            throw new UnauthorizedAccessException($"Access denied to namespace {namespaceId}");

        var result = await Repository.GetLiterals(namespaceId, envVarNames, organizationId);
        return result;
    }
}