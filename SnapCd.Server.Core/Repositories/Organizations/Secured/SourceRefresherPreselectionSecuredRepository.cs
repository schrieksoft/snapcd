using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.SourceRefresherPreselections;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class SourceRefresherPreselectionSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<SourceRefresherPreselectionRepositorySettings> options)
{
    public SourceRefresherPreselectionSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new SourceRefresherPreselectionSecuredRepository(
            new SourceRefresherPreselectionRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class SourceRefresherPreselectionSecuredRepository : GenericOrganizationChildSecuredRepository<
    SourceRefresherPreselection,
    SourceRefresherPreselectionReadDto,
    SourceRefresherPreselectionRepository,
    SourceRefresherPreselectionCreatedEvent,
    SourceRefresherPreselectionUpdatedEvent,
    SourceRefresherPreselectionDeletedEvent,
    SourceRefresherPreselectionRepositorySettings>
{
    public SourceRefresherPreselectionSecuredRepository(
        SourceRefresherPreselectionRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }


    public async Task<SourceRefresherPreselection> GetBySourceUrl(string sourceUrl, Guid organizationId)
    {
        var entity = await Repository.GetBySourceUrl(sourceUrl, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"{nameof(Stack)} with organization ID {organizationId} and SourceUrl {sourceUrl} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read it.");

        return entity;
    }
}