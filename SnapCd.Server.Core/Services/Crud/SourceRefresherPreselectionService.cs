// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.SourceRefresherPreselections;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class SourceRefresherPreselectionService : GenericCrudService<
    SourceRefresherPreselection,
    SourceRefresherPreselectionCreateDto,
    SourceRefresherPreselectionUpdateDto,
    SourceRefresherPreselectionReadDto,
    SourceRefresherPreselectionSecuredRepository,
    SourceRefresherPreselectionRepository,
    SourceRefresherPreselectionCreatedEvent,
    SourceRefresherPreselectionUpdatedEvent,
    SourceRefresherPreselectionDeletedEvent,
    SourceRefresherPreselectionRepositorySettings>
{
    public SourceRefresherPreselectionService(
        SourceRefresherPreselectionSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override SourceRefresherPreselection MapToEntity(SourceRefresherPreselectionCreateDto dto, Guid organizationId)
    {
        return SourceRefresherPreselectionMapper.ToEntity(dto, organizationId);
    }

    protected override SourceRefresherPreselectionReadDto MapToDto(SourceRefresherPreselection entity)
    {
        return SourceRefresherPreselectionMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(SourceRefresherPreselection entity, SourceRefresherPreselectionUpdateDto dto)
    {
        SourceRefresherPreselectionMapper.UpdateEntity(entity, dto);
    }

    public async Task<SourceRefresherPreselectionReadDto> GetBySourceUrl(string sourceUrl, Guid organizationId)
    {
        return await GetByCriteria(repo => repo.GetBySourceUrl(sourceUrl, organizationId));
    }
}