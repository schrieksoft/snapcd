using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.ModuleInputs.Base;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class ModuleInputSecuredRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<ModuleInputRepositorySettings> options)
{
    public ModuleInputSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleInputSecuredRepository(
            new ModuleInputRepository(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class ModuleInputSecuredRepository : GenericModuleChildSecuredRepository<
    ModuleInput,
    ModuleInputReadDto,
    ModuleInputRepository,
    ModuleInputCreatedEvent,
    ModuleInputUpdatedEvent,
    ModuleInputDeletedEvent,
    ModuleInputRepositorySettings>
{
    public ModuleInputSecuredRepository(
        ModuleInputRepository repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public async Task<ModuleInput> Get(Guid moduleId, string name, Guid organizationId)
    {
        var entity = await Repository.Get(moduleId, name, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to {nameof(ModuleInput)} {entity.Id}");

        return entity;
    }
}