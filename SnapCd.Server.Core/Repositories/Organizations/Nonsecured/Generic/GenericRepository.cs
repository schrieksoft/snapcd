using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Repository.Organization.Base;
using SnapCd.Server.Core.Mappers.Repositories;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Interfaces;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;

public record QuotaCheckResult(bool IsExceeded, int CurrentCount, int Limit);

public abstract class GenericRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TSettings> : IDisposable
    where TEntity : class, IEntity
    where TCreateEvent : CreatedEvent<TDto>, new()
    where TUpdateEvent : UpdatedEvent<TDto>, new()
    where TDeleteEvent : DeletedEvent<TDto>, new()
    where TSettings : class, IEntitySettings
{
    public readonly SnapCdDbContext DbContext;
    protected readonly IPrincipalProvider PrincipalProvider;
    protected readonly IPublishEndpoint Bus;
    protected readonly IOptions<TSettings> Options;
    protected readonly QuotaService? QuotaService;

    private bool _deferEvents;
    private readonly List<Func<Task>> _pendingEvents = new();

    public GenericRepository(SnapCdDbContext dbContext, IPrincipalProvider principalProvider, IPublishEndpoint bus, IOptions<TSettings> options, QuotaService? quotaService = null)
    {
        DbContext = dbContext;
        PrincipalProvider = principalProvider;
        Bus = bus;
        Options = options;
        QuotaService = quotaService;
    }

    /// <summary>
    /// Helper method for derived classes to check quota using QuotaService.
    /// </summary>
    protected async Task<QuotaCheckResult> CheckQuotaWithServiceAsync(Guid organizationId, string quotaName, int currentCount)
    {
        if (QuotaService == null)
        {
            return new QuotaCheckResult(false, 0, 0); // No quota enforcement if service not available
        }

        var quotaLimit = await QuotaService.GetQuotaAsync(organizationId, quotaName);

        if (quotaLimit == null)
        {
            return new QuotaCheckResult(false, currentCount, 0); // Unlimited
        }

        return new QuotaCheckResult(currentCount >= quotaLimit.Value, currentCount, quotaLimit.Value);
    }

    public void Dispose()
    {
        DbContext.Dispose();
    }

    protected virtual List<object> AdditionalCreateMessages(TEntity entity) => new();
    protected virtual List<object> AdditionalUpdateMessages(TEntity entity) => new();
    protected virtual List<object> AdditionalDeleteMessages(TEntity entity) => new();

    protected virtual Task<QuotaCheckResult> CheckQuotaAsync(TEntity entity)
    {
        // Default: no quota enforcement. Override in derived class and use CheckQuotaWithServiceAsync.
        return Task.FromResult(new QuotaCheckResult(false, 0, 0));
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

    private async Task PublishAdditionalMessages(List<object> messages)
    {
        foreach (var message in messages)
            await EnqueueOrPublish(() => Bus.Publish(message,
                publishContext => { publishContext.TimeToLive = Options.Value.EventTtl; }));
    }

    public virtual async Task<TEntity> Get(
        Guid id,
        Guid organizationId,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryModifier = null)
    {
        var query = DbContext.Set<TEntity>().AsQueryable();

        if (queryModifier != null) query = queryModifier(query);

        var entity = query
            .FirstOrDefault(m => m.Id == id && m.OrganizationId == organizationId);

        if (entity == null)
            throw new EntityNotFoundException($"{typeof(TEntity).Name} with ID {id} not found.");

        return entity;
    }
    

    public virtual async Task<TProjection> Get<TProjection>(
        Guid id,
        Guid organizationId,
        Func<IQueryable<TEntity>, IQueryable<TProjection>> queryModifier)
    {
        var query = DbContext.Set<TEntity>()
            .Where(m => m.Id == id && m.OrganizationId == organizationId);

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

    public virtual async Task<TEntity> ExecuteCreate(TEntity entity)
    {
        if (entity.Id == Guid.Empty)
            throw new IdIsEmptyException($"{typeof(TEntity).Name} ID cannot be empty.");

        if (entity.OrganizationId == Guid.Empty)
            throw new OrganizationIdIsEmptyException();

        // Check quota before creating
        var quotaResult = await CheckQuotaAsync(entity);
        if (quotaResult is { IsExceeded: true })
        {
            throw new QuotaExceededException(
                typeof(TEntity).Name,
                quotaResult.CurrentCount,
                quotaResult.Limit,
                $"{typeof(TEntity).Name} quota exceeded. Current: {quotaResult.CurrentCount}, Limit: {quotaResult.Limit}");
        }

        var principalId = PrincipalProvider.GetSubjectOrDefault(entity.OrganizationId);
        var principalDiscriminator = PrincipalProvider.GetPrincipalDiscriminatorOrDefault();
        var auditDiscriminator = ConvertToAuditPrincipalDiscriminator(principalDiscriminator);

        // Set audit fields
        
        var now = DateTime.UtcNow;
        
        entity.CreatedBy = principalId;
        entity.CreatedByPrincipalDiscriminator = auditDiscriminator;
        entity.CreatedDateTime = now;
        entity.ModifiedBy = principalId;
        entity.ModifiedByPrincipalDiscriminator = auditDiscriminator;
        entity.ModifiedDateTime = now;

        DbContext.Set<TEntity>().Add(entity);

        // Only set owner if we have a real principal (not system)
        if (principalId != Guid.Empty && principalDiscriminator.HasValue) await MaybeSetOwner(entity.Id, entity.OrganizationId, principalId, principalDiscriminator.Value);

        await DbContext.SaveChangesAsync();

        if (Options.Value.EmitCreateEvents)
        {
            await EnqueueOrPublish(() => Bus.Publish(EventMapper.ToCreateEto<TEntity, TDto, TCreateEvent>(entity, MapToDto, entity.OrganizationId),
                publishContext => { publishContext.TimeToLive = Options.Value.EventTtl; }));
            await PublishAdditionalMessages(AdditionalCreateMessages(entity));
        }

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
        if (entity.Id == Guid.Empty)
            throw new IdIsEmptyException($"{typeof(TEntity).Name} ID cannot be empty.");

        var existingEntity = DbContext.Set<TEntity>()
            .FirstOrDefault(m => m.Id == entity.Id && m.OrganizationId == entity.OrganizationId);

        if (existingEntity == null)
            throw new EntityNotFoundException($"{typeof(TEntity).Name} with ID {entity.Id} not found.");

        // Capture the previous state before updating
        var previousEntity = CloneEntity(existingEntity);

        var principalId = PrincipalProvider.GetSubjectOrDefault(entity.OrganizationId);
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
        {
            await EnqueueOrPublish(() => Bus.Publish(EventMapper.ToUpdateEto<TEntity, TDto, TUpdateEvent>(previousEntity, existingEntity, MapToDto, entity.OrganizationId),
                publishContext => { publishContext.TimeToLive = Options.Value.EventTtl; }));
            await PublishAdditionalMessages(AdditionalUpdateMessages(entity));
        }

        return existingEntity;
    }

    protected virtual async Task DeleteInTransaction(Guid id, Guid organizationId)
    {
        _deferEvents = true;
        await using var transaction = await DbContext.Database.BeginTransactionAsync();
        try
        {
            await ExecuteDelete(id, organizationId);
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

    public async Task Delete(Guid id, Guid organizationId)
    {
        // This method is not *virtual* on purpose. Override the "ExecuteDelete" method
        await DeleteInTransaction(id, organizationId);
    }

    public virtual async Task ExecuteDelete(Guid id, Guid organizationId)
    {
        var entity = DbContext.Set<TEntity>()
            .FirstOrDefault(m => m.Id == id && m.OrganizationId == organizationId);

        if (entity == null) throw new EntityNotFoundException($"{typeof(TEntity).Name} with ID {id} not found.");

        // Clone the entity before deletion for the event
        var deletedEntity = CloneEntity(entity);

        DbContext.Set<TEntity>().Remove(entity);
        await DbContext.SaveChangesAsync();

        if (Options.Value.EmitDeleteEvents)
        {
            await EnqueueOrPublish(() => Bus.Publish(EventMapper.ToDeleteEto<TEntity, TDto, TDeleteEvent>(deletedEntity, MapToDto, organizationId),
                publishContext => { publishContext.TimeToLive = Options.Value.EventTtl; }));
            await PublishAdditionalMessages(AdditionalDeleteMessages(entity));
        }
    }

    public virtual async Task<int> Count(
        Guid organizationId,
        IQueryable<TEntity>? query = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryModifier = null)
    {
        query ??= DbContext.Set<TEntity>();

        query = query.Where(m => m.OrganizationId == organizationId);

        if (queryModifier != null) query = queryModifier(query);

        return query.Count();
    }


    public virtual async Task<List<TEntity>> List(
        Guid organizationId,
        IQueryable<TEntity>? query = null,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryModifier = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null)
    {
        query ??= DbContext.Set<TEntity>();

        query = query.Where(m => m.OrganizationId == organizationId);

        if (queryModifier != null) query = queryModifier(query);

        if (orderBy != null) query = orderBy(query);

        if (pageNumber.HasValue && pageSize.HasValue) query = query.Skip((pageNumber.Value - 1) * pageSize.Value).Take(pageSize.Value);

        return query.Distinct().ToList();
    }

    public virtual async Task<List<TProjection>> List<TProjection>(
        Guid organizationId,
        Func<IQueryable<TEntity>, IQueryable<TProjection>> projection,
        IQueryable<TEntity>? query = null,
        Func<IQueryable<TProjection>, IOrderedQueryable<TProjection>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null)
    {
        query ??= DbContext.Set<TEntity>();
        query = query.Where(m => m.OrganizationId == organizationId);

        var projected = projection(query);

        if (orderBy != null) projected = orderBy(projected);

        if (pageNumber.HasValue && pageSize.HasValue) projected = projected.Skip((pageNumber.Value - 1) * pageSize.Value).Take(pageSize.Value);

        return projected.Distinct().ToList();
    }

    public virtual async Task<List<TEntity>> ListByParentId(
        Guid parentId,
        Guid organizationId,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryModifier = null,
        IQueryable<TEntity>? query = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null
    )
    {
        var parentFilter = ByParentIdQueryModifier(parentId);
        var originalModifier = queryModifier;
        queryModifier = q =>
        {
            var filtered = parentFilter(q);
            return originalModifier != null ? originalModifier(filtered) : filtered;
        };

        return await List(
            organizationId,
            query,
            queryModifier,
            orderBy,
            pageNumber,
            pageSize);
    }

    public virtual async Task<List<TProjection>> ListByParentId<TProjection>(
        Guid parentId,
        Guid organizationId,
        Func<IQueryable<TEntity>, IQueryable<TProjection>> projection,
        IQueryable<TEntity>? query = null,
        Func<IQueryable<TProjection>, IOrderedQueryable<TProjection>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null
    )
    {
        query ??= DbContext.Set<TEntity>();
        query = query.Where(m => m.OrganizationId == organizationId);

        // Apply parent ID filter
        query = ByParentIdQueryModifier(parentId)(query);

        var projected = projection(query);

        if (orderBy != null) projected = orderBy(projected);

        if (pageNumber.HasValue && pageSize.HasValue) projected = projected.Skip((pageNumber.Value - 1) * pageSize.Value).Take(pageSize.Value);

        return projected.Distinct().ToList();
    }

    protected abstract Func<IQueryable<TEntity>, IQueryable<TEntity>> ByParentIdQueryModifier(Guid parentId);

    /// <summary>
    /// Maps an entity to its DTO representation. Must be implemented by derived classes.
    /// </summary>
    protected abstract TDto MapToDto(TEntity entity);

    protected virtual async Task MaybeSetOwner(Guid id, Guid organizationId, Guid principalId, PrincipalDiscriminator principalDiscriminator)
    {
        switch (principalDiscriminator)
        {
            case PrincipalDiscriminator.User:
                await SetUserOwner(id, organizationId, principalId);
                break;
            case PrincipalDiscriminator.ServicePrincipal:
                await SetServicePrincipalOwner(id, organizationId, principalId);
                break;
            default:
                throw new InvalidOperationException($"Unsupported principal discriminator: {principalDiscriminator}");
        }
    }


    protected virtual async Task SetServicePrincipalOwner(Guid id, Guid organizationId, Guid servicePrincipalId)
    {
        // by default do nothing. Override in concrete class if owner needs to be set.
    }

    protected virtual async Task SetUserOwner(Guid id, Guid organizationId, Guid userId)
    {
        // by default do nothing. Override in concrete class if owner needs to be set.
    }

    protected AuditPrincipalDiscriminator ConvertToAuditPrincipalDiscriminator(PrincipalDiscriminator? principalDiscriminator)
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