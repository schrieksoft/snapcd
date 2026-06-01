// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.NamespacePulumiFlags;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class NamespacePulumiFlagService : GenericCrudService<NamespacePulumiFlag, NamespacePulumiFlagCreateDto, NamespacePulumiFlagUpdateDto, NamespacePulumiFlagReadDto, NamespacePulumiFlagSecuredRepository, NamespacePulumiFlagRepository,
    NamespacePulumiFlagCreatedEvent, NamespacePulumiFlagUpdatedEvent, NamespacePulumiFlagDeletedEvent, NamespacePulumiFlagRepositorySettings>
{
    public NamespacePulumiFlagService(
        NamespacePulumiFlagSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override NamespacePulumiFlag MapToEntity(NamespacePulumiFlagCreateDto dto, Guid organizationId)
    {
        return NamespacePulumiFlagMapper.ToEntity(dto, organizationId);
    }

    protected override NamespacePulumiFlagReadDto MapToDto(NamespacePulumiFlag entity)
    {
        return NamespacePulumiFlagMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(NamespacePulumiFlag entity, NamespacePulumiFlagUpdateDto dto)
    {
        NamespacePulumiFlagMapper.UpdateEntity(entity, dto);
    }
}
