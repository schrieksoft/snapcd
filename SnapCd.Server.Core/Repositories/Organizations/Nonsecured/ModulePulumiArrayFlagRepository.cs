using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.ModulePulumiArrayFlags;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class ModulePulumiArrayFlagRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ModulePulumiArrayFlagRepositorySettings> options)
{
    public ModulePulumiArrayFlagRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModulePulumiArrayFlagRepository(dbContext, principalProvider, bus, options);
    }
}

public class ModulePulumiArrayFlagRepository : GenericModuleChildDefinitionRepository<ModulePulumiArrayFlag, ModulePulumiArrayFlagReadDto, ModulePulumiArrayFlagCreatedEvent, ModulePulumiArrayFlagUpdatedEvent,
    ModulePulumiArrayFlagDeletedEvent, ModulePulumiArrayFlagRepositorySettings>
{
    public ModulePulumiArrayFlagRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ModulePulumiArrayFlagRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ModulePulumiArrayFlagReadDto MapToDto(ModulePulumiArrayFlag entity)
    {
        return ModulePulumiArrayFlagMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(ModulePulumiArrayFlag entity)
    {
        var currentCount = await DbContext.ModulePulumiArrayFlags
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.ModulePulumiArrayFlagQuota), currentCount);
    }
}
