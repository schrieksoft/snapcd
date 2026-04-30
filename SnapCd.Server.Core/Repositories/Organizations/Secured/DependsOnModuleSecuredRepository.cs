using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.DependsOnModules;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class DependsOnModuleSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<DependsOnModuleRepositorySettings> options)
{
    public DependsOnModuleSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new DependsOnModuleSecuredRepository(
            new DependsOnModuleRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class DependsOnModuleSecuredRepository : GenericModuleChildSecuredRepository<
    DependsOnModule,
    DependsOnModuleReadDto,
    DependsOnModuleRepository,
    DependsOnModuleCreatedEvent,
    DependsOnModuleUpdatedEvent,
    DependsOnModuleDeletedEvent,
    DependsOnModuleRepositorySettings>
{
    public DependsOnModuleSecuredRepository(
        DependsOnModuleRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public async Task<DependsOnModule> Get(Guid moduleId, Guid dependsOnModuleId, Guid organizationId)
    {
        var entity = await Repository.Get(moduleId, dependsOnModuleId, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to DependsOnModule {entity.Id}");

        return entity;
    }
}