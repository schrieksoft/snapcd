using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.Secrets.Scoped;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Secrets;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.Secrets.Scoped;

public class StackSecretSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<StackSecretRepositorySettings> options)
{
    public StackSecretSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new StackSecretSecuredRepository(
            new StackSecretRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class StackSecretSecuredRepository : GenericStackChildSecuredRepository<
    StackSecret,
    StackSecretDto,
    StackSecretRepository,
    StackSecretCreatedEvent,
    StackSecretUpdatedEvent,
    StackSecretDeletedEvent,
    StackSecretRepositorySettings>
{
    public StackSecretSecuredRepository(
        StackSecretRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public async Task<StackSecret> GetByName(string name, Guid organizationId,
        Func<IQueryable<StackSecret>, IQueryable<StackSecret>>? include = null)
    {
        var secret = await Repository.GetByName(name, include);

        if (!CanRead(secret.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to StackSecret {secret.Id}");

        return secret;
    }

    public async Task<List<StackSecret>> ListByIds(List<Guid> ids, Guid organizationId)
    {
        var secrets = await Repository.ListByIds(ids, organizationId);

        foreach (var secret in secrets)
            if (!CanRead(secret.Id, organizationId))
                throw new UnauthorizedAccessException($"Access denied to StackSecret {secret.Id}");

        return secrets;
    }
}