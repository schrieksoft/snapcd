using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured;

public class ModuleInputFromOutputSetSecuredRepositoryFactory<TEntity>(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<ModuleInputFromOutputSetRepositorySettings> options)
    where TEntity : ModuleInput, IModuleInputFromOutputSet
{
    public ModuleInputFromOutputSetSecuredRepository<TEntity> Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleInputFromOutputSetSecuredRepository<TEntity>(
            new ModuleInputFromOutputSetRepository<TEntity>(dbContext, principalProvider, bus, options),
            principalProvider);
    }
}

public class ModuleInputFromOutputSetSecuredRepository<TEntity> : GenericModuleChildSecuredRepository<
    TEntity,
    ModuleInputFromOutputSetReadDto,
    ModuleInputFromOutputSetRepository<TEntity>,
    ModuleInputFromOutputSetCreatedEvent,
    ModuleInputFromOutputSetUpdatedEvent,
    ModuleInputFromOutputSetDeletedEvent,
    ModuleInputFromOutputSetRepositorySettings>
    where TEntity : ModuleInput, IModuleInputFromOutputSet
{
    public ModuleInputFromOutputSetSecuredRepository(
        ModuleInputFromOutputSetRepository<TEntity> repository,
        IPrincipalProvider principalProvider)
        : base(repository, principalProvider)
    {
    }

    public async Task<TEntity> Get(Guid moduleId, string name, Guid organizationId)
    {
        var entity = await Repository.Get(moduleId, name, organizationId);

        if (!CanRead(entity.Id, organizationId))
            throw new UnauthorizedAccessException($"Access denied to {typeof(TEntity).Name} {entity.Id}");

        return entity;
    }
}