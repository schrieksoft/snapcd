// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.System;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Repository.System.Base;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.System.Nonsecured;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Interfaces;

namespace SnapCd.Server.Core.Repositories.System.Secured;

public class GenericSystemSecuredRepositoryFactory<TEntity, TDto, TRepository, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions>(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<TOptions> options)
    where TEntity : class, ISystemEntity
    where TRepository : GenericSystemRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions>
    where TCreateEvent : SystemCreatedEvent<TDto>, new()
    where TUpdateEvent : SystemUpdatedEvent<TDto>, new()
    where TDeleteEvent : SystemDeletedEvent<TDto>, new()
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

public abstract class GenericSystemSecuredRepository<TEntity, TDto, TRepository, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions> : IDisposable
    where TEntity : class, ISystemEntity
    where TRepository : GenericSystemRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TOptions>
    where TCreateEvent : SystemCreatedEvent<TDto>, new()
    where TUpdateEvent : SystemUpdatedEvent<TDto>, new()
    where TDeleteEvent : SystemDeletedEvent<TDto>, new()
    where TOptions : class, IEntitySettings

{
    public readonly TRepository SystemRepository;
    public readonly IPrincipalProvider PrincipalProvider;
    public readonly PrincipalDiscriminator PrincipalDiscriminator;

    public GenericSystemSecuredRepository(TRepository systemRepository, IPrincipalProvider principalProvider)
    {
        SystemRepository = systemRepository;
        PrincipalProvider = principalProvider;
        PrincipalDiscriminator = PrincipalProvider.GetPrincipalDiscriminator();
    }

    public virtual void Dispose()
    {
        SystemRepository.Dispose();
    }

    public virtual async Task<TEntity> Get(
        Guid id,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryModifier = null)
    {
        if (!CanRead(id))
            throw new PrincipalNotAuthorizedException(
                $"{typeof(TEntity).Name} with ID {id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSystemSubject()} does not have permission to read it.");

        return await SystemRepository.Get(id, queryModifier);
    }

    public virtual async Task<TProjection> Get<TProjection>(
        Guid id,
        Func<IQueryable<TEntity>, IQueryable<TProjection>> queryModifier)
    {
        if (!CanRead(id))
            throw new PrincipalNotAuthorizedException(
                $"{typeof(TEntity).Name} with ID {id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSystemSubject()} does not have permission to read it.");

        return await SystemRepository.Get(id, queryModifier);
    }

    public virtual async Task<int> Count(
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryModifier = null)
    {
        return await SystemRepository.Count(ReadQuery(), queryModifier);
    }

    public virtual async Task<List<TEntity>> List(
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryModifier = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null)
    {
        return await SystemRepository.List(ReadQuery(), queryModifier, orderBy, pageNumber, pageSize);
    }

    public virtual async Task<List<TProjection>> List<TProjection>(
        Func<IQueryable<TEntity>, IQueryable<TProjection>> queryModifier,
        Func<IQueryable<TProjection>, IOrderedQueryable<TProjection>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null)
    {
        return await SystemRepository.List(queryModifier, ReadQuery(), orderBy, pageNumber, pageSize);
    }

    public virtual async Task<List<TEntity>> ListByParentId(
        Guid parentId,
        Func<IQueryable<TEntity>, IQueryable<TEntity>>? queryModifier = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null
    )
    {
        return await SystemRepository.ListByParentId(parentId, queryModifier, ReadQuery(), orderBy, pageNumber, pageSize);
    }

    public virtual async Task<TEntity> Create(TEntity entity)
    {
        if (!CanCreate())
            throw new PrincipalNotAuthorizedException(
                $"{PrincipalDiscriminator} with ID {PrincipalProvider.GetSystemSubject()} does not have permission to create a {typeof(TEntity).Name}");

        return await SystemRepository.Create(entity);
    }

    public virtual async Task<TEntity> Update(TEntity entity)
    {
        if (!CanUpdate(entity.Id))
            throw new PrincipalNotAuthorizedException(
                $"{typeof(TEntity).Name} with ID {entity.Id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSystemSubject()} does not have permission to update it.");

        return await SystemRepository.Update(entity);
    }

    public virtual async Task Delete(Guid id)
    {
        if (!CanDelete(id))
            throw new PrincipalNotAuthorizedException(
                $"{typeof(TEntity).Name} with ID {id} not found or {PrincipalDiscriminator} with ID {PrincipalProvider.GetSystemSubject()} does not have permission to delete it.");

        await SystemRepository.Delete(id);
    }

    public virtual List<SystemRole> ReadRoles => new() { SystemRole.Administrator };
    public virtual List<SystemRole> UpdateRoles => new() { SystemRole.Administrator };
    public virtual List<SystemRole> CreateRoles => new() { SystemRole.Administrator };
    public virtual List<SystemRole> DeleteRoles => new() { SystemRole.Administrator };


    public virtual IQueryable<TEntity> CreateQuery()
    {
        return RoleQueryDispatch(CreateRoles);
    }

    public virtual IQueryable<TEntity> ReadQuery()
    {
        return RoleQueryDispatch(ReadRoles);
    }

    public virtual IQueryable<TEntity> UpdateQuery()
    {
        return RoleQueryDispatch(UpdateRoles);
    }

    public virtual IQueryable<TEntity> DeleteQuery()
    {
        return RoleQueryDispatch(DeleteRoles);
    }


    protected virtual IQueryable<TEntity> RoleQueryDispatch(List<SystemRole> systemRoles)
    {
        var principalId = PrincipalProvider.GetSystemSubject();

        return PrincipalDiscriminator switch
        {
            PrincipalDiscriminator.User => RoleQuery<UserSystemRoleAssignment>(principalId, systemRoles),
            PrincipalDiscriminator.ServicePrincipal => RoleQuery<ServicePrincipalSystemRoleAssignment>(principalId, systemRoles),
            _ => throw new InvalidOperationException($"Unsupported principal discriminator: {PrincipalDiscriminator}")
        };
    }


    protected abstract IQueryable<TEntity> RoleQuery<TSystemRoleAssignment>(
        Guid principalId,
        List<SystemRole> systemRoles)
        where TSystemRoleAssignment : class, ISystemRoleAssignment;

    public virtual bool CanRead(Guid id)
    {
        return ReadQuery().Any();
    }

    public virtual bool CanCreate()
    {
        return CreateQuery().Any();
    }

    public virtual bool CanUpdate(Guid id)
    {
        return UpdateQuery().Any();
    }

    public virtual bool CanDelete(Guid id)
    {
        return DeleteQuery().Any();
    }
}