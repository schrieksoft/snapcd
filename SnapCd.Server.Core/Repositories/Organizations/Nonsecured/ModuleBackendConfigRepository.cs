using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.ModuleBackendConfigs;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class ModuleBackendConfigRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ModuleBackendConfigRepositorySettings> options)
{
    public ModuleBackendConfigRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleBackendConfigRepository(dbContext, principalProvider, bus, options);
    }
}

public class ModuleBackendConfigRepository : GenericModuleChildDefinitionRepository<ModuleBackendConfig, ModuleBackendConfigReadDto, ModuleBackendConfigCreatedEvent, ModuleBackendConfigUpdatedEvent,
    ModuleBackendConfigDeletedEvent, ModuleBackendConfigRepositorySettings>
{
    public ModuleBackendConfigRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ModuleBackendConfigRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ModuleBackendConfigReadDto MapToDto(ModuleBackendConfig entity)
    {
        return ModuleBackendConfigMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(ModuleBackendConfig entity)
    {
        var currentCount = await DbContext.ModuleBackendConfigs
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.ModuleBackendConfigQuota), currentCount);
    }

    public async Task<ModuleBackendConfig> Get(Guid moduleId, string name, Guid organizationId)
    {
        var entity = await DbContext.ModuleBackendConfigs
            .SingleOrDefaultAsync(i => i.Name == name && i.ModuleId == moduleId && i.OrganizationId == organizationId);

        if (entity == null)
            throw new EntityNotFoundException($"ModuleBackendConfig with name {name} not found for module {moduleId}.");

        return entity;
    }
}