// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModuleAdditionalTriggerPaths;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class ModuleAdditionalTriggerPathService : GenericCrudService<ModuleAdditionalTriggerPath, ModuleAdditionalTriggerPathCreateDto, ModuleAdditionalTriggerPathUpdateDto, ModuleAdditionalTriggerPathReadDto,
    ModuleAdditionalTriggerPathSecuredRepository, ModuleAdditionalTriggerPathRepository, ModuleAdditionalTriggerPathCreatedEvent,
    ModuleAdditionalTriggerPathUpdatedEvent, ModuleAdditionalTriggerPathDeletedEvent, ModuleAdditionalTriggerPathRepositorySettings>
{
    public ModuleAdditionalTriggerPathService(
        ModuleAdditionalTriggerPathSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override ModuleAdditionalTriggerPath MapToEntity(ModuleAdditionalTriggerPathCreateDto dto, Guid organizationId)
    {
        return ModuleAdditionalTriggerPathMapper.ToEntity(dto, organizationId);
    }

    protected override ModuleAdditionalTriggerPathReadDto MapToDto(ModuleAdditionalTriggerPath entity)
    {
        return ModuleAdditionalTriggerPathMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(ModuleAdditionalTriggerPath entity, ModuleAdditionalTriggerPathUpdateDto dto)
    {
        ModuleAdditionalTriggerPathMapper.UpdateEntity(entity, dto);
    }

    public async Task<ModuleAdditionalTriggerPathReadDto> Get(Guid moduleId, string path, Guid organizationId)
    {
        var entity = await SecuredRepository.Get(moduleId, path, organizationId);
        return ModuleAdditionalTriggerPathMapper.ToDto(entity);
    }
}
