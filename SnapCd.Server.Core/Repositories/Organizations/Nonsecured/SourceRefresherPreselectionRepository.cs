using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.SourceRefresherPreselections;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class SourceRefresherPreselectionRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<SourceRefresherPreselectionRepositorySettings> options)
{
    public SourceRefresherPreselectionRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new SourceRefresherPreselectionRepository(dbContext, principalProvider, bus, options);
    }
}

public class SourceRefresherPreselectionRepository : GenericOrganizationChildRepository<SourceRefresherPreselection, SourceRefresherPreselectionReadDto, SourceRefresherPreselectionCreatedEvent,
    SourceRefresherPreselectionUpdatedEvent, SourceRefresherPreselectionDeletedEvent, SourceRefresherPreselectionRepositorySettings>
{
    public SourceRefresherPreselectionRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<SourceRefresherPreselectionRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override SourceRefresherPreselectionReadDto MapToDto(SourceRefresherPreselection entity)
    {
        return SourceRefresherPreselectionMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(SourceRefresherPreselection entity)
    {
        var currentCount = await DbContext.SourceRefresherPreselections
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.SourceRefresherPreselectionQuota), currentCount);
    }

    public async Task<SourceRefresherPreselection> GetBySourceUrl(string sourceUrl, Guid organizationId)
    {
        var entity = await DbContext.SourceRefresherPreselections
            .Include(x => x.Runner)
            .SingleOrDefaultAsync(i => i.SourceUrl == sourceUrl && i.OrganizationId == organizationId);

        if (entity == null) throw new EntityNotFoundException($"SourceRefresherPreselection with SourceUrl {sourceUrl} not found.");

        return entity;
    }
}