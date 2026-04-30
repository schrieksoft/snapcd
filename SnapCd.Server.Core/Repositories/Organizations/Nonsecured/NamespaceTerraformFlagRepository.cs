using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.NamespaceTerraformFlags;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class NamespaceTerraformFlagRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<NamespaceTerraformFlagRepositorySettings> options)
{
    public NamespaceTerraformFlagRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespaceTerraformFlagRepository(dbContext, principalProvider, bus, options);
    }
}

public class NamespaceTerraformFlagRepository : GenericNamespaceChildDefinitionRepository<NamespaceTerraformFlag, NamespaceTerraformFlagReadDto, NamespaceTerraformFlagCreatedEvent,
    NamespaceTerraformFlagUpdatedEvent, NamespaceTerraformFlagDeletedEvent, NamespaceTerraformFlagRepositorySettings>
{
    public NamespaceTerraformFlagRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<NamespaceTerraformFlagRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override NamespaceTerraformFlagReadDto MapToDto(NamespaceTerraformFlag entity)
    {
        return NamespaceTerraformFlagMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(NamespaceTerraformFlag entity)
    {
        var currentCount = await DbContext.NamespaceTerraformFlags
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.NamespaceTerraformFlagQuota), currentCount);
    }
}
