// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModulePulumiArrayFlags;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Views;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Services.Crud;

public class ModulePulumiArrayFlagService : GenericCrudService<ModulePulumiArrayFlag, ModulePulumiArrayFlagMetadata, ModulePulumiArrayFlagCreateDto, ModulePulumiArrayFlagUpdateDto, ModulePulumiArrayFlagReadDto, ModulePulumiArrayFlagMetadataReadDto, ModulePulumiArrayFlagSecuredRepository, ModulePulumiArrayFlagRepository,
    ModulePulumiArrayFlagCreatedEvent, ModulePulumiArrayFlagUpdatedEvent, ModulePulumiArrayFlagDeletedEvent, ModulePulumiArrayFlagRepositorySettings>
{
    public ModulePulumiArrayFlagService(
        ModulePulumiArrayFlagSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override ModulePulumiArrayFlag MapToEntity(ModulePulumiArrayFlagCreateDto dto, Guid organizationId)
    {
        return ModulePulumiArrayFlagMapper.ToEntity(dto, organizationId);
    }

    protected override ModulePulumiArrayFlagReadDto MapToDto(ModulePulumiArrayFlag entity)
    {
        return ModulePulumiArrayFlagMapper.ToDto(entity);
    }

    protected override ModulePulumiArrayFlagMetadataReadDto MapToMetadataDto(ModulePulumiArrayFlagMetadata view)
    {
        return ModulePulumiArrayFlagMapper.ToMetadataDto(view);
    }

    protected override void UpdateEntityFromDto(ModulePulumiArrayFlag entity, ModulePulumiArrayFlagUpdateDto dto)
    {
        ModulePulumiArrayFlagMapper.UpdateEntity(entity, dto);
    }
}
