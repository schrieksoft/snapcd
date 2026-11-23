// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Entities.Definition.Base;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Interfaces.Crud;
using SnapCd.Server.Core.Services.Secured.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Services.Secured.Crud;

public class ModuleInputUnsetService<TEntity> : GenericCrudService<
    TEntity,
    ModuleInputUnsetMetadata,
    ModuleInputUnsetCreateDto,
    ModuleInputUnsetUpdateDto,
    ModuleInputUnsetReadDto,
    ModuleInputUnsetMetadataReadDto,
    ModuleInputUnsetSecuredRepository<TEntity>,
    ModuleInputUnsetRepository<TEntity>,
    ModuleInputUnsetCreatedEvent,
    ModuleInputUnsetUpdatedEvent,
    ModuleInputUnsetDeletedEvent,
    ModuleInputUnsetRepositorySettings>, IModuleInputUnsetService
    where TEntity : ModuleInput, IModuleInputUnset, new()
{
    public ModuleInputUnsetService(
        ModuleInputUnsetSecuredRepository<TEntity> securedRepository
    ) : base(securedRepository)
    {
    }

    protected override TEntity MapToEntity(ModuleInputUnsetCreateDto dto, Guid organizationId)
    {
        return ModuleInputUnsetMapper.ToEntity<TEntity>(dto, organizationId);
    }

    protected override ModuleInputUnsetReadDto MapToDto(TEntity entity)
    {
        return ModuleInputUnsetMapper.ToDto(entity);
    }

    protected override ModuleInputUnsetMetadataReadDto MapToMetadataDto(ModuleInputUnsetMetadata view)
    {
        return ModuleInputUnsetMapper.ToMetadataDto(view);
    }

    protected override void UpdateEntityFromDto(TEntity entity, ModuleInputUnsetUpdateDto dto)
    {
        ModuleInputUnsetMapper.UpdateEntity(entity, dto);
    }

    public async Task<ModuleInputUnsetReadDto> Get(Guid moduleId, string name, Guid organizationId)
    {
        return await GetByCriteria(repo => repo.Get(moduleId, name, organizationId));
    }
}