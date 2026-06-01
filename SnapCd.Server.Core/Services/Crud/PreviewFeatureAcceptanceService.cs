// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Dtos.PreviewFeatureAcceptances;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class PreviewFeatureAcceptanceService : GenericCrudService<
    PreviewFeatureAcceptance,
    PreviewFeatureAcceptanceCreateDto,
    PreviewFeatureAcceptanceUpdateDto,
    PreviewFeatureAcceptanceReadDto,
    PreviewFeatureAcceptanceSecuredRepository,
    PreviewFeatureAcceptanceRepository,
    PreviewFeatureAcceptanceCreatedEvent,
    PreviewFeatureAcceptanceUpdatedEvent,
    PreviewFeatureAcceptanceDeletedEvent,
    PreviewFeatureAcceptanceRepositorySettings>
{
    public PreviewFeatureAcceptanceService(
        PreviewFeatureAcceptanceSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override PreviewFeatureAcceptance MapToEntity(PreviewFeatureAcceptanceCreateDto dto, Guid organizationId)
    {
        return PreviewFeatureAcceptanceMapper.ToEntity(dto, organizationId);
    }

    protected override PreviewFeatureAcceptanceReadDto MapToDto(PreviewFeatureAcceptance entity)
    {
        return PreviewFeatureAcceptanceMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(PreviewFeatureAcceptance entity, PreviewFeatureAcceptanceUpdateDto dto)
    {
        PreviewFeatureAcceptanceMapper.UpdateEntity(entity, dto);
    }

    public async Task<bool> HasAccepted(PreviewFeature feature, Guid organizationId)
    {
        var entity = await SecuredRepository.GetByFeature(feature, organizationId);
        return entity != null;
    }

    public async Task<List<PreviewFeatureAcceptanceReadDto>> ListAccepted(Guid organizationId)
    {
        var entities = await SecuredRepository.ListByOrganization(organizationId);
        return entities.Select(MapToDto).ToList();
    }
}
