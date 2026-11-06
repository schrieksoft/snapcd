// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.ModuleOutputDownstreamRestrictions;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Secured.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Services.Secured.Crud;

public class ModuleOutputDownstreamRestrictionService : GenericCrudService<
    ModuleOutputDownstreamRestriction, ModuleOutputDownstreamRestrictionMetadata,
    ModuleOutputDownstreamRestrictionCreateDto,
    ModuleOutputDownstreamRestrictionUpdateDto,
    ModuleOutputDownstreamRestrictionReadDto,
    ModuleOutputDownstreamRestrictionMetadataReadDto,
    ModuleOutputDownstreamRestrictionSecuredRepository,
    ModuleOutputDownstreamRestrictionRepository,
    ModuleOutputDownstreamRestrictionCreatedEvent,
    ModuleOutputDownstreamRestrictionUpdatedEvent,
    ModuleOutputDownstreamRestrictionDeletedEvent,
    ModuleOutputDownstreamRestrictionRepositorySettings>
{
    public ModuleOutputDownstreamRestrictionService(
        ModuleOutputDownstreamRestrictionSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override ModuleOutputDownstreamRestriction MapToEntity(ModuleOutputDownstreamRestrictionCreateDto dto, Guid organizationId)
    {
        return ModuleOutputDownstreamRestrictionMapper.ToEntity(dto, organizationId);
    }

    protected override ModuleOutputDownstreamRestrictionReadDto MapToDto(ModuleOutputDownstreamRestriction entity)
    {
        return ModuleOutputDownstreamRestrictionMapper.ToDto(entity);
    }

    protected override ModuleOutputDownstreamRestrictionMetadataReadDto MapToMetadataDto(ModuleOutputDownstreamRestrictionMetadata view)
    {
        return ModuleOutputDownstreamRestrictionMapper.ToMetadataDto(view);
    }

    protected override void UpdateEntityFromDto(ModuleOutputDownstreamRestriction entity, ModuleOutputDownstreamRestrictionUpdateDto dto)
    {
        ModuleOutputDownstreamRestrictionMapper.UpdateEntity(entity, dto);
    }
}
