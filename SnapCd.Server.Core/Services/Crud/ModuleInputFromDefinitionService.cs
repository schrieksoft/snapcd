// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Entities.Interfaces;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Services.Crud.Interfaces;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class ModuleInputFromDefinitionService<TEntity> : GenericCrudService<
    TEntity,
    ModuleInputFromDefinitionCreateDto,
    ModuleInputFromDefinitionUpdateDto,
    ModuleInputFromDefinitionReadDto,
    ModuleInputFromDefinitionSecuredRepository<TEntity>,
    ModuleInputFromDefinitionRepository<TEntity>,
    ModuleInputFromDefinitionCreatedEvent,
    ModuleInputFromDefinitionUpdatedEvent,
    ModuleInputFromDefinitionDeletedEvent,
    ModuleInputFromDefinitionRepositorySettings>, IModuleInputFromDefinitionService
    where TEntity : Entities.Definition.Base.ModuleInput, IModuleInputFromDefinition, new()
{
    public ModuleInputFromDefinitionService(
        ModuleInputFromDefinitionSecuredRepository<TEntity> securedRepository
    ) : base(securedRepository)
    {
    }

    protected override TEntity MapToEntity(ModuleInputFromDefinitionCreateDto dto, Guid organizationId)
    {
        return ModuleInputFromDefinitionMapper.ToEntity<TEntity>(dto, organizationId);
    }

    protected override ModuleInputFromDefinitionReadDto MapToDto(TEntity entity)
    {
        return ModuleInputFromDefinitionMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(TEntity entity, ModuleInputFromDefinitionUpdateDto dto)
    {
        ModuleInputFromDefinitionMapper.UpdateEntity(entity, dto);
    }

    public async Task<ModuleInputFromDefinitionReadDto> Get(Guid moduleId, string name, Guid organizationId)
    {
        return await GetByCriteria(repo => repo.Get(moduleId, name, organizationId));
    }
}