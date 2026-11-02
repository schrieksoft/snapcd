// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.OutputModuleSupplies;
using SnapCd.Server.Core.Entities.Definition.OutputSupplies;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Secured.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Services.Secured.Crud;

public class OutputModuleSupplyService : GenericCrudService<
    OutputModuleSupply, OutputModuleSupplyMetadata,
    OutputModuleSupplyCreateDto,
    OutputModuleSupplyUpdateDto,
    OutputModuleSupplyReadDto,
    OutputModuleSupplyMetadataReadDto,
    OutputModuleSupplySecuredRepository,
    OutputModuleSupplyRepository,
    OutputModuleSupplyCreatedEvent,
    OutputModuleSupplyUpdatedEvent,
    OutputModuleSupplyDeletedEvent,
    OutputModuleSupplyRepositorySettings>
{
    public OutputModuleSupplyService(
        OutputModuleSupplySecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override OutputModuleSupply MapToEntity(OutputModuleSupplyCreateDto dto, Guid organizationId)
    {
        return OutputModuleSupplyMapper.ToEntity(dto, organizationId);
    }

    protected override OutputModuleSupplyReadDto MapToDto(OutputModuleSupply entity)
    {
        return OutputModuleSupplyMapper.ToDto(entity);
    }

    protected override OutputModuleSupplyMetadataReadDto MapToMetadataDto(OutputModuleSupplyMetadata view)
    {
        return OutputModuleSupplyMapper.ToMetadataDto(view);
    }

    protected override void UpdateEntityFromDto(OutputModuleSupply entity, OutputModuleSupplyUpdateDto dto)
    {
        OutputModuleSupplyMapper.UpdateEntity(entity, dto);
    }
}
