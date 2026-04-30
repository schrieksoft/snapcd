using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.NamespaceBackendConfigs;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class NamespaceBackendConfigRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<NamespaceBackendConfigRepositorySettings> options)
{
    public NamespaceBackendConfigRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespaceBackendConfigRepository(dbContext, principalProvider, bus, options);
    }
}

public class NamespaceBackendConfigRepository : GenericNamespaceChildDefinitionRepository<NamespaceBackendConfig, NamespaceBackendConfigReadDto, NamespaceBackendConfigCreatedEvent,
    NamespaceBackendConfigUpdatedEvent, NamespaceBackendConfigDeletedEvent, NamespaceBackendConfigRepositorySettings>
{
    public NamespaceBackendConfigRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<NamespaceBackendConfigRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override NamespaceBackendConfigReadDto MapToDto(NamespaceBackendConfig entity)
    {
        return NamespaceBackendConfigMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(NamespaceBackendConfig entity)
    {
        var currentCount = await DbContext.NamespaceBackendConfigs
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.NamespaceBackendConfigQuota), currentCount);
    }

    public async Task<NamespaceBackendConfig> Get(Guid namespaceId, string name, Guid organizationId)
    {
        var entity = await DbContext.NamespaceBackendConfigs
            .SingleOrDefaultAsync(i => i.Name == name && i.NamespaceId == namespaceId && i.OrganizationId == organizationId);

        if (entity == null)
            throw new EntityNotFoundException($"NamespaceBackendConfig with name {name} not found for namespace {namespaceId}.");

        return entity;
    }
}