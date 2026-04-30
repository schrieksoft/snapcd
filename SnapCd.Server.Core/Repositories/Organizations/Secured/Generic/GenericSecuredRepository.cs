using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization.Base;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Misc.Helpers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Interfaces;

namespace SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;

public class GenericSecuredRepositoryFactory<TEntity, TDto, TRepository, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions>(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<TOptions> options)
    where TEntity : class, IEntity
    where TRepository : GenericRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions>
    where TCreateEvent : CreatedEvent<TDto>, new()
    where TUpdateEvent : UpdatedEvent<TDto>, new()
    where TDeleteEvent : DeletedEvent<TDto>, new()
    where TOptions : class, IEntitySettings
{
    public TRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return (TRepository)Activator.CreateInstance(typeof(TRepository), dbContext, principalProvider, bus, options)!;
    }
}

public abstract class GenericSecuredRepository<TEntity, TDto, TRepository, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions> : IDisposable
    where TEntity : class, IEntity
    where TRepository : GenericRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions>
    where TCreateEvent : CreatedEvent<TDto>, new()
    where TUpdateEvent : UpdatedEvent<TDto>, new()
    where TDeleteEvent : DeletedEvent<TDto>, new()
    where TOptions : class, IEntitySettings

{
    public readonly TRepository Repository;
    public readonly IPrincipalProvider PrincipalProvider;
    public readonly PrincipalDiscriminator PrincipalDiscriminator;


    public GenericSecuredRepository(TRepository repository, IPrincipalProvider principalProvider)
    {
        Repository = repository;
        PrincipalProvider = principalProvider;
        PrincipalDiscriminator = PrincipalProvider.GetPrincipalDiscriminator();
    }


    public virtual void Dispose()
    {
        Repository.Dispose();
    }

    public virtual async Task<TEntity> Get(
        Guid id,
        Guid organizationId,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryModifier = null)
    {
        if (!CanRead(id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"{typeof(TEntity).Name} with ID {id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read it.");

        return await Repository.Get(id, organizationId, queryModifier);
    }

    public virtual async Task<TProjection> Get<TProjection>(
        Guid id,
        Guid organizationId,
        Func<IQueryable<TEntity>, IQueryable<TProjection>> queryModifier)
    {
        if (!CanRead(id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"{typeof(TEntity).Name} with ID {id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to read it.");

        return await Repository.Get(id, organizationId, queryModifier);
    }

    public virtual async Task<int> Count(
        Guid organizationId,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryModifier = null)
    {
        return await Repository.Count(organizationId, ReadQuery(organizationId), queryModifier);
    }

    public virtual async Task<List<TEntity>> List(
        Guid organizationId,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryModifier = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null)
    {
        return await Repository.List(organizationId, ReadQuery(organizationId), queryModifier, orderBy, pageNumber, pageSize);
    }

    public virtual async Task<List<TProjection>> List<TProjection>(
        Guid organizationId,
        Func<IQueryable<TEntity>, IQueryable<TProjection>> queryModifier,
        Func<IQueryable<TProjection>, IOrderedQueryable<TProjection>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null)
    {
        return await Repository.List(organizationId, queryModifier, ReadQuery(organizationId), orderBy, pageNumber, pageSize);
    }

    public virtual async Task<List<TEntity>> ListByParentId(
        Guid parentId,
        Guid organizationId,
        Func<IQueryable<TEntity>, IQueryable<TEntity>> queryModifier = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null
    )
    {
        return await Repository.ListByParentId(parentId, organizationId, queryModifier, ReadQuery(organizationId), orderBy, pageNumber, pageSize);
    }

    public virtual async Task<List<TProjection>> ListByParentId<TProjection>(
        Guid parentId,
        Guid organizationId,
        Func<IQueryable<TEntity>, IQueryable<TProjection>> projection,
        Func<IQueryable<TProjection>, IOrderedQueryable<TProjection>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null
    )
    {
        return await Repository.ListByParentId(parentId, organizationId, projection, ReadQuery(organizationId), orderBy, pageNumber, pageSize);
    }

    public virtual async Task<TEntity> Create(TEntity entity, bool inTransaction = true)
    {
        if (!CanCreate(entity.ParentId(), entity.OrganizationId))
            throw new PrincipalNotAuthorizedException(
                $"{GetParentEntityName()} with ID {entity.Id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(entity.OrganizationId)} does not have permission to create a {typeof(TEntity).Name} within it");

        if (inTransaction)
            return await Repository.Create(entity);
        else
            return await Repository.ExecuteCreate(entity);
    }

    public virtual async Task<TEntity> Update(TEntity entity, bool inTransaction = true)
    {
        if (!CanUpdate(entity.Id, entity.OrganizationId))
            throw new PrincipalNotAuthorizedException(
                $"{typeof(TEntity).Name} with ID {entity.Id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(entity.OrganizationId)} does not have permission to update it.");
       
        if (inTransaction)
            return await Repository.Update(entity);
        else
            return await Repository.ExecuteUpdate(entity);
        
    }

    public virtual async Task Delete(Guid id, Guid organizationId, bool inTransaction = true)
    {
        if (!CanDelete(id, organizationId))
            throw new PrincipalNotAuthorizedException(
                $"{typeof(TEntity).Name} with ID {id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSubject(organizationId)} does not have permission to delete it.");

        if (inTransaction)
            await Repository.Delete(id, organizationId);
        else
            await Repository.ExecuteDelete(id, organizationId);
        
    }

    public virtual PermissionMap ReadPermissionMap => new();
    public virtual PermissionMap UpdatePermissionMap => new();
    public virtual PermissionMap CreatePermissionMap => new();
    public virtual PermissionMap DeletePermissionMap => new();


    public abstract IQueryable<TEntity> CreateQuery(Guid organizationId);
    public abstract IQueryable<TEntity> ReadQuery(Guid organizationId);
    public abstract IQueryable<TEntity> UpdateQuery(Guid organizationId);
    public abstract IQueryable<TEntity> DeleteQuery(Guid organizationId);

    public abstract bool CanRead(Guid id, Guid organizationId);

    public abstract bool CanCreate(Guid parentId, Guid organizationId);

    public abstract bool CanUpdate(Guid id, Guid organizationId);

    public abstract bool CanDelete(Guid id, Guid organizationId);

    public abstract string GetParentEntityName();
}