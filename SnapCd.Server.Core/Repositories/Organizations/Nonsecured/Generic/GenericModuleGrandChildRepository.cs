// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization.Base;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Interfaces;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;

/// <summary>
/// Base repository for entities that are grandchildren of Modules.
/// Examples: Input (child of VariableSet), Output (child of OutputSet)
///
/// The hierarchy is: Module -> ModuleChild (e.g., VariableSet) -> ModuleGrandChild (e.g., Input)
/// </summary>
/// <typeparam name="TEntity">The entity type (e.g., Input, Output)</typeparam>
/// <typeparam name="TEntityParent">The parent entity type that is a child of Module (e.g., VariableSet, OutputSet)</typeparam>
/// <typeparam name="TDto">The DTO type for the entity</typeparam>
/// <typeparam name="TCreateEvent">The create event type</typeparam>
/// <typeparam name="TUpdateEvent">The update event type</typeparam>
/// <typeparam name="TDeleteEvent">The delete event type</typeparam>
/// <typeparam name="TSettings">The repository settings type</typeparam>
public abstract class GenericModuleGrandChildRepository<TEntity, TEntityParent, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TSettings>
    : GenericRepository<TEntity, TDto, TCreateEvent, TUpdateEvent, TDeleteEvent, TSettings>
    where TEntity : class, IEntity
    where TEntityParent : class, IEntity, IModuleChild
    where TCreateEvent : CreatedEvent<TDto>, new()
    where TUpdateEvent : UpdatedEvent<TDto>, new()
    where TDeleteEvent : DeletedEvent<TDto>, new()
    where TSettings : class, IEntitySettings
{
    protected GenericModuleGrandChildRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<TSettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    /// <summary>
    /// Gets the parent entity ID property name for the entity.
    /// For example, Input has VariableSetId, Output has OutputSetId.
    /// This must be implemented by concrete repositories.
    /// </summary>
    protected abstract Func<TEntity, Guid> ParentIdAccessor { get; }

    /// <summary>
    /// Gets the DbSet property accessor for the parent entity type.
    /// For example, for Input this would return the VariableSets DbSet.
    /// </summary>
    protected abstract Func<SnapCdDbContext, Microsoft.EntityFrameworkCore.DbSet<TEntityParent>> ParentDbSetAccessor { get; }

    /// <summary>
    /// Provides query modifier to filter by parent ID (the ModuleChild ID, e.g., VariableSetId).
    /// This is used for listing entities by their immediate parent.
    /// </summary>
    protected override Func<IQueryable<TEntity>, IQueryable<TEntity>> ByParentIdQueryModifier(Guid parentId)
    {
        return query => query.Where(e => ParentIdAccessor(e) == parentId);
    }

    /// <summary>
    /// Provides query modifier to filter entities by the grandparent Module ID.
    /// For example, to list all Inputs for a given Module (via VariableSet).
    /// </summary>
    public virtual Func<IQueryable<TEntity>, IQueryable<TEntity>> ByModuleIdQueryModifier(Guid moduleId)
    {
        return query =>
        {
            var parentDbSet = ParentDbSetAccessor(DbContext);
            return from entity in query
                join parent in parentDbSet
                    on ParentIdAccessor(entity) equals parent.Id
                where parent.ModuleId == moduleId
                select entity;
        };
    }

    /// <summary>
    /// Lists entities by their grandparent Module ID.
    /// </summary>
    public virtual async Task<List<TEntity>> ListByModuleId(
        Guid moduleId,
        Guid organizationId,
        IQueryable<TEntity>? query = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        int? pageNumber = null,
        int? pageSize = null)
    {
        var queryModifier = ByModuleIdQueryModifier(moduleId);

        return await List(
            organizationId,
            query,
            queryModifier,
            orderBy,
            pageNumber,
            pageSize);
    }
}