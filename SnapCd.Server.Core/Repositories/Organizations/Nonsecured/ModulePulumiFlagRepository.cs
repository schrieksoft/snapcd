using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.ModulePulumiFlags;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class ModulePulumiFlagRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ModulePulumiFlagRepositorySettings> options)
{
    public ModulePulumiFlagRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModulePulumiFlagRepository(dbContext, principalProvider, bus, options);
    }
}

public class ModulePulumiFlagRepository : GenericModuleChildDefinitionRepository<ModulePulumiFlag, ModulePulumiFlagReadDto, ModulePulumiFlagCreatedEvent, ModulePulumiFlagUpdatedEvent,
    ModulePulumiFlagDeletedEvent, ModulePulumiFlagRepositorySettings>
{
    public ModulePulumiFlagRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ModulePulumiFlagRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ModulePulumiFlagReadDto MapToDto(ModulePulumiFlag entity)
    {
        return ModulePulumiFlagMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(ModulePulumiFlag entity)
    {
        var currentCount = await DbContext.ModulePulumiFlags
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.ModulePulumiFlagQuota), currentCount);
    }
}
