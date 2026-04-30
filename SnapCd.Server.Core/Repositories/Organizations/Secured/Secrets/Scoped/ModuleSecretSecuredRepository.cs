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

public class ModuleSecretSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<ModuleSecretRepositorySettings> options)
{
    public ModuleSecretSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleSecretSecuredRepository(
            new ModuleSecretRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class ModuleSecretSecuredRepository : GenericModuleChildSecuredRepository<
    ModuleSecret,
    ModuleSecretDto,
    ModuleSecretRepository,
    ModuleSecretCreatedEvent,
    ModuleSecretUpdatedEvent,
    ModuleSecretDeletedEvent,
    ModuleSecretRepositorySettings>
{
    public ModuleSecretSecuredRepository(
        ModuleSecretRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public async Task<ModuleSecret> GetByName(string name, Guid organizationId,
        Func<IQueryable<ModuleSecret>, IQueryable<ModuleSecret>>? include = null)
    {
        var secret = await Repository.GetByName(name, include);

        if (!CanRead(secret.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to ModuleSecret {secret.Id}");

        return secret;
    }

    public async Task<List<ModuleSecret>> ListByIds(List<Guid> ids, Guid organizationId)
    {
        var secrets = await Repository.ListByIds(ids, organizationId);

        foreach (var secret in secrets)
            if (!CanRead(secret.Id, organizationId))
                throw new UnauthorizedAccessException($"Access denied to ModuleSecret {secret.Id}");

        return secrets;
    }
}