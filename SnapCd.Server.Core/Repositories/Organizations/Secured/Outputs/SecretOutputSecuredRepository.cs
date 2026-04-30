using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Outputs;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Outputs;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.Outputs;

public class SecretOutputSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<OutputRepositorySettings> options)
{
    public SecretOutputSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new SecretOutputSecuredRepository(
            new SecretOutputRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class SecretOutputSecuredRepository : OutputSecuredRepository
{
    private new SecretOutputRepository Repository => (SecretOutputRepository)base.Repository;

    public SecretOutputSecuredRepository(
        SecretOutputRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public async Task<SecretOutput> GetByName(string name, Guid organizationId,
        Func<IQueryable<SecretOutput>, IQueryable<SecretOutput>>? include = null)
    {
        var secret = await Repository.GetByName(name, include);

        if (!CanRead(secret.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to SecretOutput {secret.Id}");

        return secret;
    }

    public async Task<List<SecretOutput>> ListByIds(List<Guid> ids, Guid organizationId)
    {
        var secrets = await Repository.ListByIds(ids, organizationId);

        foreach (var secret in secrets)
            if (!CanRead(secret.Id, organizationId))
                throw new UnauthorizedAccessException($"Access denied to SecretOutput {secret.Id}");

        return secrets;
    }
}