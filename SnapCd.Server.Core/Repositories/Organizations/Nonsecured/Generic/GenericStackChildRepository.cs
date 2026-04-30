using MassTransit;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization.Base;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Interfaces;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;

public abstract class
    GenericStackChildRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TSettings> : GenericRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TSettings>
    where TEntity : class, IEntity, IStackChild
    where TCreateEvent : CreatedEvent<TDto>, new()
    where TUpdateEvent : UpdatedEvent<TDto>, new()
    where TDeleteEvent : DeletedEvent<TDto>, new()
    where TSettings : class, IEntitySettings
{
    public GenericStackChildRepository(SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<TSettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override Func<IQueryable<TEntity>, IQueryable<TEntity>> ByParentIdQueryModifier(Guid stackId)
    {
        return query => query.Where(e => e.StackId == stackId);
    }
}