// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModuleHooks;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class ModuleHookService : GenericCrudService<ModuleHook, ModuleHookCreateDto, ModuleHookUpdateDto, ModuleHookReadDto, ModuleHookSecuredRepository, ModuleHookRepository,
    ModuleHookCreatedEvent, ModuleHookUpdatedEvent, ModuleHookDeletedEvent, ModuleHookRepositorySettings>
{
    public ModuleHookService(
        ModuleHookSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override ModuleHook MapToEntity(ModuleHookCreateDto dto, Guid organizationId)
    {
        return ModuleHookMapper.ToEntity(dto, organizationId);
    }

    protected override ModuleHookReadDto MapToDto(ModuleHook entity)
    {
        return ModuleHookMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(ModuleHook entity, ModuleHookUpdateDto dto)
    {
        ModuleHookMapper.UpdateEntity(entity, dto);
    }
}
