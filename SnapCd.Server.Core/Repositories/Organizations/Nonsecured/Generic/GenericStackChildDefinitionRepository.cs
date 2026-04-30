using MassTransit;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization.Base;
using SnapCd.Server.Core.Events.System;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Interfaces;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;

public abstract class
    GenericStackChildDefinitionRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TSettings> : GenericStackChildRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent,
    TSettings>
    where TEntity : class, IEntity, IStackChild
    where TCreateEvent : CreatedEvent<TDto>, new()
    where TUpdateEvent : UpdatedEvent<TDto>, new()
    where TDeleteEvent : DeletedEvent<TDto>, new()
    where TSettings : class, IEntitySettings
{
    public GenericStackChildDefinitionRepository(SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<TSettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    // This repository is specifically for "StackChild" classes that are regarded as "Definition" entities (Inputs, Secrets, DependsonModule etc.) so that we can trigger specific events for
    // those.


    protected override Func<IQueryable<TEntity>, IQueryable<TEntity>> ByParentIdQueryModifier(Guid stackId)
    {
        return query => query.Where(e => e.StackId == stackId);
    }

    protected override List<object> AdditionalCreateMessages(TEntity entity)
    {
        var messages = new List<object>();
        messages.Add(new StackModifiedEvent { Id = entity.StackId, OrganizationId = entity.OrganizationId });
        return messages;
    }

    protected override List<object> AdditionalUpdateMessages(TEntity entity)
    {
        var messages = new List<object>();
        messages.Add(new StackModifiedEvent { Id = entity.StackId, OrganizationId = entity.OrganizationId });
        return messages;
    }

    protected override List<object> AdditionalDeleteMessages(TEntity entity)
    {
        var messages = new List<object>();
        messages.Add(new StackModifiedEvent { Id = entity.StackId, OrganizationId = entity.OrganizationId });
        return messages;
    }
}