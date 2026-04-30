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
    GenericModuleChildDefinitionRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TSettings> : GenericModuleChildRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent,
    TSettings>
    where TEntity : class, IEntity, IModuleChild
    where TCreateEvent : CreatedEvent<TDto>, new()
    where TUpdateEvent : UpdatedEvent<TDto>, new()
    where TDeleteEvent : DeletedEvent<TDto>, new()
    where TSettings : class, IEntitySettings
{
    // This repository is specifically for "ModuleChild" classes that are regarded as "Definition" entities (Inputs, Secrets, DependsonModule etc.) so that we can trigger specific events for
    // those.

    public GenericModuleChildDefinitionRepository(SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<TSettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }


    protected override List<object> AdditionalCreateMessages(TEntity entity)
    {
        var messages = new List<object>();
        messages.Add(new ModuleModifiedEvent { Id = entity.ModuleId, OrganizationId = entity.OrganizationId });
        return messages;
    }

    protected override List<object> AdditionalUpdateMessages(TEntity entity)
    {
        var messages = new List<object>();
        messages.Add(new ModuleModifiedEvent { Id = entity.ModuleId, OrganizationId = entity.OrganizationId });
        return messages;
    }

    protected override List<object> AdditionalDeleteMessages(TEntity entity)
    {
        var messages = new List<object>();
        messages.Add(new ModuleModifiedEvent { Id = entity.ModuleId, OrganizationId = entity.OrganizationId });
        return messages;
    }
}