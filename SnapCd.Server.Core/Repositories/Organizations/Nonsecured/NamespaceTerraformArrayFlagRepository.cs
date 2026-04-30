using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.NamespaceTerraformArrayFlags;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class NamespaceTerraformArrayFlagRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<NamespaceTerraformArrayFlagRepositorySettings> options)
{
    public NamespaceTerraformArrayFlagRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespaceTerraformArrayFlagRepository(dbContext, principalProvider, bus, options);
    }
}

public class NamespaceTerraformArrayFlagRepository : GenericNamespaceChildDefinitionRepository<NamespaceTerraformArrayFlag, NamespaceTerraformArrayFlagReadDto, NamespaceTerraformArrayFlagCreatedEvent,
    NamespaceTerraformArrayFlagUpdatedEvent, NamespaceTerraformArrayFlagDeletedEvent, NamespaceTerraformArrayFlagRepositorySettings>
{
    public NamespaceTerraformArrayFlagRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<NamespaceTerraformArrayFlagRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override NamespaceTerraformArrayFlagReadDto MapToDto(NamespaceTerraformArrayFlag entity)
    {
        return NamespaceTerraformArrayFlagMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(NamespaceTerraformArrayFlag entity)
    {
        var currentCount = await DbContext.NamespaceTerraformArrayFlags
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.NamespaceTerraformArrayFlagQuota), currentCount);
    }
}
