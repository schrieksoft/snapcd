// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization.Base;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Generic;
using SnapCd.Server.Core.Settings.Interfaces;
using SnapCd.Server.Core.Views;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Services.Crud.Generic;

public abstract class GenericCrudService<TEntity, TMetadata, TCreateDto, TUpdateDto, TDto, TSecuredRepository, TRepository, TCreateEvent, TUpdateEvent, TDeleteEvent, TSettings> : IDisposable
    where TEntity : class, IEntity
    where TMetadata : EntityMetadataBase
    where TCreateDto : class
    where TUpdateDto : class
    where TDto : class
    where TRepository : GenericRepository<TEntity, TDto, TMetadata, TCreateEvent, TUpdateEvent, TDeleteEvent, TSettings>
    where TSecuredRepository : GenericSecuredRepository<TEntity, TDto, TMetadata, TRepository, TCreateEvent, TUpdateEvent, TDeleteEvent, TSettings>
    where TCreateEvent : CreatedEvent<TDto>, new()
    where TUpdateEvent : UpdatedEvent<TDto>, new()
    where TDeleteEvent : DeletedEvent<TDto>, new()
    where TSettings : class, IEntitySettings

{
    protected readonly TSecuredRepository SecuredRepository;

    public GenericCrudService(TSecuredRepository securedRepository)
    {
        SecuredRepository = securedRepository;
    }

    /// <summary>Whether the caller may read this entity. For a dialog that wants to say so before acting.</summary>
    public bool CanRead(Guid id, Guid organizationId) => SecuredRepository.CanRead(id, organizationId);

    public bool CanUpdate(Guid id, Guid organizationId) => SecuredRepository.CanUpdate(id, organizationId);

    public bool CanDelete(Guid id, Guid organizationId) => SecuredRepository.CanDelete(id, organizationId);

    public bool CanCreate(Guid parentId, Guid organizationId) => SecuredRepository.CanCreate(parentId, organizationId);

    // Abstract mapping methods that each service must implement
    protected abstract TEntity MapToEntity(TCreateDto dto, Guid organizationId);
    protected abstract TDto MapToDto(TEntity entity);
    protected abstract void UpdateEntityFromDto(TEntity entity, TUpdateDto dto);

    public virtual void Dispose()
    {
        SecuredRepository.Dispose();
    }

    public virtual async Task<List<TDto>> ListByParentId(Guid parentId, Guid organizationId)
    {
        var entities = await SecuredRepository.ListByParentId(parentId, organizationId);
        return entities.Select(MapToDto).ToList();
    }


    public virtual async Task<TDto> Get(Guid id, Guid organizationId)
    {
        var entity = await SecuredRepository.Get(id, organizationId);
        return MapToDto(entity);
    }

    public virtual async Task<TDto> GetByCriteria(Func<TSecuredRepository, Task<TEntity>> criteria)
    {
        var entity = await criteria(SecuredRepository);
        return MapToDto(entity);
    }

    public virtual async Task<TDto> Create(TCreateDto dto, Guid organizationId)
    {
        var entity = MapToEntity(dto, organizationId);
        entity = await SecuredRepository.Create(entity);
        return MapToDto(entity);
    }

    public virtual async Task<List<TDto>> List(Guid organizationId)
    {
        var entities = await SecuredRepository.List(organizationId);
        return entities.Select(MapToDto).ToList();
    }

    public virtual async Task<TDto> Update(TUpdateDto dto, Guid id, Guid organizationId)
    {
        var entity = await SecuredRepository.Get(id, organizationId);
        UpdateEntityFromDto(entity, dto);
        entity = await SecuredRepository.Update(entity);
        return MapToDto(entity);
    }

    public virtual async Task Delete(Guid id, Guid organizationId)
    {
        await SecuredRepository.Delete(id, organizationId);
    }
}