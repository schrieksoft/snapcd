using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Dtos.PreviewFeatureAcceptances;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class PreviewFeatureAcceptanceRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<PreviewFeatureAcceptanceRepositorySettings> options)
{
    public PreviewFeatureAcceptanceRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new PreviewFeatureAcceptanceRepository(dbContext, principalProvider, bus, options);
    }
}

public class PreviewFeatureAcceptanceRepository : GenericOrganizationChildRepository<PreviewFeatureAcceptance, PreviewFeatureAcceptanceReadDto, PreviewFeatureAcceptanceCreatedEvent, PreviewFeatureAcceptanceUpdatedEvent, PreviewFeatureAcceptanceDeletedEvent, PreviewFeatureAcceptanceRepositorySettings>
{
    public PreviewFeatureAcceptanceRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<PreviewFeatureAcceptanceRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override PreviewFeatureAcceptanceReadDto MapToDto(PreviewFeatureAcceptance entity)
    {
        return PreviewFeatureAcceptanceMapper.ToDto(entity);
    }

    public async Task<PreviewFeatureAcceptance?> GetByFeature(PreviewFeature feature, Guid organizationId)
    {
        return await DbContext.PreviewFeatureAcceptances
            .Where(p => p.OrganizationId == organizationId && p.PreviewFeature == feature)
            .SingleOrDefaultAsync();
    }

    public async Task<List<PreviewFeatureAcceptance>> ListByOrganization(Guid organizationId)
    {
        return await DbContext.PreviewFeatureAcceptances
            .Where(p => p.OrganizationId == organizationId)
            .ToListAsync();
    }
}
