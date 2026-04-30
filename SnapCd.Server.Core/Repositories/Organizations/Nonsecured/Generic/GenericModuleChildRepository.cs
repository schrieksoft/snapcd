using MassTransit;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization.Base;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Interfaces;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;

public abstract class
    GenericModuleChildRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TSettings> : GenericRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TSettings>
    where TEntity : class, IEntity, IModuleChild
    where TCreateEvent : CreatedEvent<TDto>, new()
    where TUpdateEvent : UpdatedEvent<TDto>, new()
    where TDeleteEvent : DeletedEvent<TDto>, new()
    where TSettings : class, IEntitySettings
{
    public GenericModuleChildRepository(SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<TSettings> options, QuotaService? quotaService = null)
        : base(dbContext, principalProvider, bus, options, quotaService)
    {
    }

    protected override Func<IQueryable<TEntity>, IQueryable<TEntity>> ByParentIdQueryModifier(Guid moduleId)
    {
        return query => query.Where(e => e.ModuleId == moduleId);
    }
}