using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Repository.System.Base;
using SnapCd.Server.Core.Mappers.Repositories;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Interfaces;

namespace SnapCd.Server.Core.Repositories.System.Nonsecured;

public abstract class GenericSystemRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TSettings> : IDisposable
    where TEntity : class, ISystemEntity
    where TCreateEvent : SystemCreatedEvent<TDto>, new()
    where TUpdateEvent : SystemUpdatedEvent<TDto>, new()
    where TDeleteEvent : SystemDeletedEvent<TDto>, new()
    where TSettings : class, IEntitySettings
{
    public readonly SnapCdDbContext DbContext;
    protected readonly IPrincipalProvider PrincipalProvider;
    protected readonly IPublishEndpoint Bus;
    protected readonly IOptions<TSettings> Options;

    private bool _deferEvents;
    private readonly List<Func<Task>> _pendingEvents = new();

    public GenericSystemRepository(SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<TSettings> options)
    {
        DbContext = dbContext;
        PrincipalProvider = principalProvider;
        Bus = bus;
        Options = options;
    }

    public void Dispose()
    {
        DbContext.Dispose();
    }

    protected async Task EnqueueOrPublish(Func<Task> publishAction)
    {
        if (_deferEvents)
            _pendingEvents.Add(publishAction);
        else
            await publishAction();
    }

    private async Task FlushPendingEvents()
    {
        foreach (var action in _pendingEvents)
            await action();
        _pendingEvents.Clear();
    }

    public virtual async Task<TEntity> Get(
        Guid id,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryModifier = null)
    {
        var query = DbContext.Set<TEntity>().AsQueryable();

        if (queryModifier != null) query = queryModifier(query);

        var entity = query
            .FirstOrDefault(m => m.Id == id);

        if (entity == null)
            throw new EntityNotFoundException($"{typeof(TEntity).Name} with ID {id} not found.");

        return entity;
    }

    public virtual async Task<TProjection> Get<TProjection>(
        Guid id,
        Func<IQueryable<TEntity>, IQueryable<TProjection>> queryModifier)
    {
        var query = DbContext.Set<TEntity>()
            .Where(m => m.Id == id);

        var projected = queryModifier(query);

        var result = projected.FirstOrDefault();

        if (result == null)
            throw new EntityNotFoundException($"{typeof(TEntity).Name} with ID {id} not found.");

        return result;
    }

    protected virtual async Task<TEntity> CreateInTransaction(TEntity entity)
    {
        _deferEvents = true;
        TEntity result;
        await using var transaction = await DbContext.Database.BeginTransactionAsync();
        try
        {
            result = await ExecuteCreate(entity);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            _deferEvents = false;
            _pendingEvents.Clear();
            throw;
        }
        _deferEvents = false;
        await FlushPendingEvents();
        return result;
    }

    public async Task<TEntity> Create(TEntity entity)
    {
        // This method is not *virtual* on purpose. Override the "ExecuteCreate" method
        return await CreateInTransaction(entity);
    }

    protected virtual async Task<TEntity> ExecuteCreate(TEntity entity)
    {
        var principalId = PrincipalProvider.GetSystemSubjectOrDefault();
        var principalDiscriminator = PrincipalProvider.GetPrincipalDiscriminatorOrDefault();
        var auditDiscriminator = ConvertToAuditPrincipalDiscriminator(principalDiscriminator);

        // Set audit fields
        entity.CreatedBy = principalId;
        entity.CreatedByPrincipalDiscriminator = auditDiscriminator;
        entity.CreatedDateTime = DateTime.UtcNow;
        entity.ModifiedBy = principalId;
        entity.ModifiedByPrincipalDiscriminator = auditDiscriminator;
        entity.ModifiedDateTime = DateTime.UtcNow;

        DbContext.Set<TEntity>().Add(entity);

        await DbContext.SaveChangesAsync();

        if (Options.Value.EmitCreateEvents)
            await EnqueueOrPublish(() => Bus.Publish(EventMapper.ToSystemCreateEto<TEntity, TDto, TCreateEvent>(entity, MapToDto),
                publishContext => { publishContext.TimeToLive = Options.Value.EventTtl; }));

        return entity;
    }

    protected virtual async Task<TEntity> UpdateInTransaction(TEntity entity)
    {
        _deferEvents = true;
        TEntity result;
        await using var transaction = await DbContext.Database.BeginTransactionAsync();
        try
        {
            result = await ExecuteUpdate(entity);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            _deferEvents = false;
            _pendingEvents.Clear();
            throw;
        }
        _deferEvents = false;
        await FlushPendingEvents();
        return result;
    }

    public async Task<TEntity> Update(TEntity entity)
    {
        // This method is not *virtual* on purpose. Override the "ExecuteUpdate" method
        return await UpdateInTransaction(entity);
    }

    public virtual async Task<TEntity> ExecuteUpdate(TEntity entity)
    {
        var existingEntity = DbContext.Set<TEntity>()
            .FirstOrDefault(m => m.Id == entity.Id);

        if (existingEntity == null)
            throw new EntityNotFoundException($"{typeof(TEntity).Name} with ID {entity.Id} not found.");

        // Capture the previous state before updating
        var previousEntity = CloneEntity(existingEntity);

        var principalId = PrincipalProvider.GetSystemSubjectOrDefault();
        var principalDiscriminator = PrincipalProvider.GetPrincipalDiscriminatorOrDefault();
        var auditDiscriminator = ConvertToAuditPrincipalDiscriminator(principalDiscriminator);

        // Preserve creation audit fields
        entity.CreatedBy = existingEntity.CreatedBy;
        entity.CreatedByPrincipalDiscriminator = existingEntity.CreatedByPrincipalDiscriminator;
        entity.CreatedDateTime = existingEntity.CreatedDateTime;

        // Update modification audit fields
        entity.ModifiedBy = principalId;
        entity.ModifiedByPrincipalDiscriminator = auditDiscriminator;
        entity.ModifiedDateTime = DateTime.UtcNow;

        // Update the existing tracked entity
        DbContext.Entry(existingEntity).CurrentValues.SetValues(entity);
        await DbContext.SaveChangesAsync();

        if (Options.Value.EmitUpdateEvents)
            await EnqueueOrPublish(() => Bus.Publish(EventMapper.ToSystemUpdateEto<TEntity, TDto, TUpdateEvent>(previousEntity, existingEntity, MapToDto),
                publishContext => { publishContext.TimeToLive = Options.Value.EventTtl; }));

        return existingEntity;
    }

    protected virtual async Task DeleteInTransaction(Guid id)
    {
        _deferEvents = true;
        await using var transaction = await DbContext.Database.BeginTransactionAsync();
        try
        {
            await ExecuteDelete(id);
            await transaction.CommitAsync();
        }
        catch
        {
            await transaction.RollbackAsync();
            _deferEvents = false;
            _pendingEvents.Clear();
            throw;
        }
        _deferEvents = false;
        await FlushPendingEvents();
    }

    public async Task Delete(Guid id)
    {
        // This method is not *virtual* on purpose. Override the "ExecuteDelete" method
        await DeleteInTransaction(id);
    }

    protected virtual async Task ExecuteDelete(Guid id)
    {
        var entity = DbContext.Set<TEntity>()
            .FirstOrDefault(m => m.Id == id);

        if (entity == null) throw new EntityNotFoundException($"{typeof(TEntity).Name} with ID {id} not found.");

        // Clone the entity before deletion for the event
        var deletedEntity = CloneEntity(entity);

        DbContext.Set<TEntity>().Remove(entity);
        await DbContext.SaveChangesAsync();

        if (Options.Value.EmitDeleteEvents)
        {
            await EnqueueOrPublish(() => Bus.Publish(EventMapper.ToSystemDeleteEto<TEntity, TDto, TDeleteEvent>(deletedEntity, MapToDto),
                publishContext => { publishContext.TimeToLive = Options.Value.EventTtl; }));
        }
    }

    public virtual async Task<int> Count(
        IQueryable<TEntity>? query = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryModifier = null)
    {
        query ??= DbContext.Set<TEntity>();

        if (queryModifier != null) query = queryModifier(query);

        return query.Count();
    }


    public virtual async Task<List<TEntity>> List(
        IQueryable<TEntity>? query = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryModifier = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null)
    {
        query ??= DbContext.Set<TEntity>();

        if (queryModifier != null) query = queryModifier(query);

        if (orderBy != null) query = orderBy(query);

        if (pageNumber.HasValue && pageSize.HasValue) query = query.Skip((pageNumber.Value - 1) * pageSize.Value).Take(pageSize.Value);

        return query.ToList();
    }

    public virtual async Task<List<TProjection>> List<TProjection>(
        Func<IQueryable<TEntity>, IQueryable<TProjection>> projection,
        IQueryable<TEntity>? query = null,
        Func<IQueryable<TProjection>, IOrderedQueryable<TProjection>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null)
    {
        query ??= DbContext.Set<TEntity>();

        var projected = projection(query);

        if (orderBy != null) projected = orderBy(projected);

        if (pageNumber.HasValue && pageSize.HasValue) projected = projected.Skip((pageNumber.Value - 1) * pageSize.Value).Take(pageSize.Value);

        return projected.ToList();
    }

    public virtual async Task<List<TEntity>> ListByParentId(
        Guid parentId,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> queryModifier = null,
        IQueryable<TEntity>? query = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null
    )
    {
        queryModifier = q => ByParentIdQueryModifier(parentId)(q);

        return await List(
            query,
            queryModifier,
            orderBy,
            pageNumber,
            pageSize);
    }

    protected abstract Func<IQueryable<TEntity>, IQueryable<TEntity>> ByParentIdQueryModifier(Guid parentId);

    /// <summary>
    /// Maps an entity to its DTO representation. Must be implemented by derived classes.
    /// </summary>
    protected abstract TDto MapToDto(TEntity entity);

    protected virtual async Task MaybeSetOwner(Guid id, Guid principalId, PrincipalDiscriminator principalDiscriminator)
    {
        switch (principalDiscriminator)
        {
            case PrincipalDiscriminator.User:
                await SetUserOwner(id, principalId);
                break;
            case PrincipalDiscriminator.ServicePrincipal:
                await SetServicePrincipalOwner(id, principalId);
                break;
            default:
                throw new InvalidOperationException($"Unsupported principal discriminator: {principalDiscriminator}");
        }
    }


    protected virtual async Task SetServicePrincipalOwner(Guid id, Guid servicePrincipalId)
    {
        // by default do nothing. Override in concrete class if owner needs to be set.
    }

    protected virtual async Task SetUserOwner(Guid id, Guid userId)
    {
        // by default do nothing. Override in concrete class if owner needs to be set.
    }

    private AuditPrincipalDiscriminator ConvertToAuditPrincipalDiscriminator(PrincipalDiscriminator? principalDiscriminator)
    {
        if (!principalDiscriminator.HasValue)
            return AuditPrincipalDiscriminator.System;

        return principalDiscriminator.Value switch
        {
            PrincipalDiscriminator.User => AuditPrincipalDiscriminator.User,
            PrincipalDiscriminator.ServicePrincipal => AuditPrincipalDiscriminator.ServicePrincipal,
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {principalDiscriminator}")
        };
    }

    private TEntity CloneEntity(TEntity entity)
    {
        // Create a shallow copy by cloning the entry's original values
        var clone = (TEntity)Activator.CreateInstance(typeof(TEntity))!;
        var entry = DbContext.Entry(entity);

        // Copy current values to the clone
        var cloneEntry = DbContext.Entry(clone);
        cloneEntry.CurrentValues.SetValues(entry.CurrentValues);

        // Detach the clone so it doesn't interfere with tracking
        cloneEntry.State = EntityState.Detached;

        return clone;
    }
}