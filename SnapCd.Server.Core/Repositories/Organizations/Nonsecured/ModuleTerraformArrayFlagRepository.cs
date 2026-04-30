using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.ModuleTerraformArrayFlags;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class ModuleTerraformArrayFlagRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ModuleTerraformArrayFlagRepositorySettings> options)
{
    public ModuleTerraformArrayFlagRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleTerraformArrayFlagRepository(dbContext, principalProvider, bus, options);
    }
}

public class ModuleTerraformArrayFlagRepository : GenericModuleChildDefinitionRepository<ModuleTerraformArrayFlag, ModuleTerraformArrayFlagReadDto, ModuleTerraformArrayFlagCreatedEvent, ModuleTerraformArrayFlagUpdatedEvent,
    ModuleTerraformArrayFlagDeletedEvent, ModuleTerraformArrayFlagRepositorySettings>
{
    public ModuleTerraformArrayFlagRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ModuleTerraformArrayFlagRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ModuleTerraformArrayFlagReadDto MapToDto(ModuleTerraformArrayFlag entity)
    {
        return ModuleTerraformArrayFlagMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(ModuleTerraformArrayFlag entity)
    {
        var currentCount = await DbContext.ModuleTerraformArrayFlags
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.ModuleTerraformArrayFlagQuota), currentCount);
    }
}
