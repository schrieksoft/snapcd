// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.NamespacePulumiArrayFlags;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class NamespacePulumiArrayFlagService : GenericCrudService<NamespacePulumiArrayFlag, NamespacePulumiArrayFlagCreateDto, NamespacePulumiArrayFlagUpdateDto, NamespacePulumiArrayFlagReadDto, NamespacePulumiArrayFlagSecuredRepository, NamespacePulumiArrayFlagRepository,
    NamespacePulumiArrayFlagCreatedEvent, NamespacePulumiArrayFlagUpdatedEvent, NamespacePulumiArrayFlagDeletedEvent, NamespacePulumiArrayFlagRepositorySettings>
{
    public NamespacePulumiArrayFlagService(
        NamespacePulumiArrayFlagSecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override NamespacePulumiArrayFlag MapToEntity(NamespacePulumiArrayFlagCreateDto dto, Guid organizationId)
    {
        return NamespacePulumiArrayFlagMapper.ToEntity(dto, organizationId);
    }

    protected override NamespacePulumiArrayFlagReadDto MapToDto(NamespacePulumiArrayFlag entity)
    {
        return NamespacePulumiArrayFlagMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(NamespacePulumiArrayFlag entity, NamespacePulumiArrayFlagUpdateDto dto)
    {
        NamespacePulumiArrayFlagMapper.UpdateEntity(entity, dto);
    }
}
