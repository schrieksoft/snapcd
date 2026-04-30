using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.Secrets.Scoped;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Secrets;

public class NamespaceSecretRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<NamespaceSecretRepositorySettings> options)
{
    public NamespaceSecretRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespaceSecretRepository(dbContext, principalProvider, bus, options);
    }
}

public class NamespaceSecretRepository : GenericNamespaceChildDefinitionRepository<
    NamespaceSecret,
    NamespaceSecretDto,
    NamespaceSecretCreatedEvent,
    NamespaceSecretUpdatedEvent,
    NamespaceSecretDeletedEvent,
    NamespaceSecretRepositorySettings>
{
    public NamespaceSecretRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<NamespaceSecretRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override NamespaceSecretDto MapToDto(NamespaceSecret entity)
    {
        return NamespaceSecretMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(NamespaceSecret entity)
    {
        var currentCount = await DbContext.NamespaceSecrets
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.NamespaceSecretQuota), currentCount);
    }

    public async Task<NamespaceSecret> GetByName(string name,
        Func<IQueryable<NamespaceSecret>, IQueryable<NamespaceSecret>>? include = null)
    {
        var query = DbContext.Set<NamespaceSecret>().AsQueryable();

        if (include != null)
            query = include(query);

        var secret = await query
            .SingleOrDefaultAsync(i => i.Name == name);

        if (secret == null)
            throw new EntityNotFoundException($"{nameof(NamespaceSecret)} with name {name} not found.");

        return secret;
    }

    public Task<List<NamespaceSecret>> ListByIds(List<Guid> ids, Guid organizationId)
    {
        var secrets = DbContext.Set<NamespaceSecret>()
            .Include(x => x.Organization)
            .Where(x => ids.Contains(x.Id) && x.OrganizationId == organizationId)
            .ToList();
        return Task.FromResult(secrets);
    }
}