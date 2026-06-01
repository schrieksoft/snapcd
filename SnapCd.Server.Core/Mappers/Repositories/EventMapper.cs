// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization.Base;
using SnapCd.Server.Core.Events.Repository.System.Base;

namespace SnapCd.Server.Core.Mappers.Repositories;

/// <summary>
/// Utility class for converting entities to Event Transfer Objects (ETOs).
/// Maps entity audit fields and delegates DTO mapping to specific mappers.
/// </summary>
public static class EventMapper
{
    /// <summary>
    /// Converts an entity to a CreateEto for create operations.
    /// </summary>
    /// <typeparam name="TEntity">The entity type (must implement IEntity)</typeparam>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TCreateEvent">The create event type (must inherit from CreateEto)</typeparam>
    /// <param name="entity">The entity to convert</param>
    /// <param name="dtoMapper">Function to convert the entity to a DTO</param>
    /// <param name="organizationId">The organization ID for multi-tenant context</param>
    /// <returns>A CreateEto containing the DTO and audit metadata</returns>
    public static TCreateEvent ToCreateEto<TEntity, TDto, TCreateEvent>(TEntity entity, Func<TEntity, TDto> dtoMapper, Guid organizationId)
        where TEntity : class, IEntity
        where TCreateEvent : CreatedEvent<TDto>, new()
    {
        return new TCreateEvent
        {
            Data = dtoMapper(entity),
            OrganizationId = organizationId,
            CreatedBy = entity.CreatedBy,
            CreatedByPrincipalDiscriminator = entity.CreatedByPrincipalDiscriminator,
            CreatedDateTime = entity.CreatedDateTime,
            ModifiedBy = entity.ModifiedBy,
            ModifiedByPrincipalDiscriminator = entity.ModifiedByPrincipalDiscriminator,
            ModifiedDateTime = entity.ModifiedDateTime
        };
    }

    /// <summary>
    /// Converts previous and current entities to an UpdateEto for update operations.
    /// </summary>
    /// <typeparam name="TEntity">The entity type (must implement IEntity)</typeparam>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TUpdateEvent">The update event type (must inherit from UpdateEto)</typeparam>
    /// <param name="previousEntity">The entity state before update</param>
    /// <param name="currentEntity">The entity state after update</param>
    /// <param name="dtoMapper">Function to convert the entity to a DTO</param>
    /// <param name="organizationId">The organization ID for multi-tenant context</param>
    /// <returns>An UpdateEto containing both previous and current state with audit metadata</returns>
    public static TUpdateEvent ToUpdateEto<TEntity, TDto, TUpdateEvent>(TEntity previousEntity, TEntity currentEntity, Func<TEntity, TDto> dtoMapper, Guid organizationId)
        where TEntity : class, IEntity
        where TUpdateEvent : UpdatedEvent<TDto>, new()
    {
        return new TUpdateEvent
        {
            PreviousData = dtoMapper(previousEntity),
            Data = dtoMapper(currentEntity),
            OrganizationId = organizationId,
            CreatedBy = currentEntity.CreatedBy,
            CreatedByPrincipalDiscriminator = currentEntity.CreatedByPrincipalDiscriminator,
            CreatedDateTime = currentEntity.CreatedDateTime,
            ModifiedBy = currentEntity.ModifiedBy,
            ModifiedByPrincipalDiscriminator = currentEntity.ModifiedByPrincipalDiscriminator,
            ModifiedDateTime = currentEntity.ModifiedDateTime,
            PreviousModifiedBy = previousEntity.ModifiedBy,
            PreviousModifiedByPrincipalDiscriminator = previousEntity.ModifiedByPrincipalDiscriminator,
            PreviousModifiedDateTime = previousEntity.ModifiedDateTime
        };
    }

    /// <summary>
    /// Converts an entity to a DeleteEto for delete operations.
    /// </summary>
    /// <typeparam name="TEntity">The entity type (must implement IEntity)</typeparam>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TDeleteEvent">The delete event type (must inherit from DeleteEto)</typeparam>
    /// <param name="entity">The entity to convert</param>
    /// <param name="dtoMapper">Function to convert the entity to a DTO</param>
    /// <param name="organizationId">The organization ID for multi-tenant context</param>
    /// <returns>A DeleteEto containing the DTO and audit metadata</returns>
    public static TDeleteEvent ToDeleteEto<TEntity, TDto, TDeleteEvent>(TEntity entity, Func<TEntity, TDto> dtoMapper, Guid organizationId)
        where TEntity : class, IEntity
        where TDeleteEvent : DeletedEvent<TDto>, new()
    {
        return new TDeleteEvent
        {
            Data = dtoMapper(entity),
            OrganizationId = organizationId,
            CreatedBy = entity.CreatedBy,
            CreatedByPrincipalDiscriminator = entity.CreatedByPrincipalDiscriminator,
            CreatedDateTime = entity.CreatedDateTime,
            ModifiedBy = entity.ModifiedBy,
            ModifiedByPrincipalDiscriminator = entity.ModifiedByPrincipalDiscriminator,
            ModifiedDateTime = entity.ModifiedDateTime
        };
    }

    /// <summary>
    /// Converts a system entity to a SystemCreateEto for create operations.
    /// </summary>
    /// <typeparam name="TEntity">The entity type (must implement ISystemEntity)</typeparam>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TCreateEvent">The create event type (must inherit from SystemCreateEto)</typeparam>
    /// <param name="entity">The entity to convert</param>
    /// <param name="dtoMapper">Function to convert the entity to a DTO</param>
    /// <returns>A SystemCreateEto containing the DTO and audit metadata</returns>
    public static TCreateEvent ToSystemCreateEto<TEntity, TDto, TCreateEvent>(TEntity entity, Func<TEntity, TDto> dtoMapper)
        where TEntity : class, ISystemEntity
        where TCreateEvent : SystemCreatedEvent<TDto>, new()
    {
        return new TCreateEvent
        {
            Data = dtoMapper(entity),
            CreatedBy = entity.CreatedBy,
            CreatedByPrincipalDiscriminator = entity.CreatedByPrincipalDiscriminator,
            CreatedDateTime = entity.CreatedDateTime,
            ModifiedBy = entity.ModifiedBy,
            ModifiedByPrincipalDiscriminator = entity.ModifiedByPrincipalDiscriminator,
            ModifiedDateTime = entity.ModifiedDateTime
        };
    }

    /// <summary>
    /// Converts previous and current system entities to a SystemUpdateEto for update operations.
    /// </summary>
    /// <typeparam name="TEntity">The entity type (must implement ISystemEntity)</typeparam>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TUpdateEvent">The update event type (must inherit from SystemUpdateEto)</typeparam>
    /// <param name="previousEntity">The entity state before update</param>
    /// <param name="currentEntity">The entity state after update</param>
    /// <param name="dtoMapper">Function to convert the entity to a DTO</param>
    /// <returns>A SystemUpdateEto containing both previous and current state with audit metadata</returns>
    public static TUpdateEvent ToSystemUpdateEto<TEntity, TDto, TUpdateEvent>(TEntity previousEntity, TEntity currentEntity, Func<TEntity, TDto> dtoMapper)
        where TEntity : class, ISystemEntity
        where TUpdateEvent : SystemUpdatedEvent<TDto>, new()
    {
        return new TUpdateEvent
        {
            PreviousData = dtoMapper(previousEntity),
            Data = dtoMapper(currentEntity),
            CreatedBy = currentEntity.CreatedBy,
            CreatedByPrincipalDiscriminator = currentEntity.CreatedByPrincipalDiscriminator,
            CreatedDateTime = currentEntity.CreatedDateTime,
            ModifiedBy = currentEntity.ModifiedBy,
            ModifiedByPrincipalDiscriminator = currentEntity.ModifiedByPrincipalDiscriminator,
            ModifiedDateTime = currentEntity.ModifiedDateTime,
            PreviousModifiedBy = previousEntity.ModifiedBy,
            PreviousModifiedByPrincipalDiscriminator = previousEntity.ModifiedByPrincipalDiscriminator,
            PreviousModifiedDateTime = previousEntity.ModifiedDateTime
        };
    }

    /// <summary>
    /// Converts a system entity to a SystemDeleteEto for delete operations.
    /// </summary>
    /// <typeparam name="TEntity">The entity type (must implement ISystemEntity)</typeparam>
    /// <typeparam name="TDto">The DTO type</typeparam>
    /// <typeparam name="TDeleteEvent">The delete event type (must inherit from SystemDeleteEto)</typeparam>
    /// <param name="entity">The entity to convert</param>
    /// <param name="dtoMapper">Function to convert the entity to a DTO</param>
    /// <returns>A SystemDeleteEto containing the DTO and audit metadata</returns>
    public static TDeleteEvent ToSystemDeleteEto<TEntity, TDto, TDeleteEvent>(TEntity entity, Func<TEntity, TDto> dtoMapper)
        where TEntity : class, ISystemEntity
        where TDeleteEvent : SystemDeletedEvent<TDto>, new()
    {
        return new TDeleteEvent
        {
            Data = dtoMapper(entity),
            CreatedBy = entity.CreatedBy,
            CreatedByPrincipalDiscriminator = entity.CreatedByPrincipalDiscriminator,
            CreatedDateTime = entity.CreatedDateTime,
            ModifiedBy = entity.ModifiedBy,
            ModifiedByPrincipalDiscriminator = entity.ModifiedByPrincipalDiscriminator,
            ModifiedDateTime = entity.ModifiedDateTime
        };
    }
}