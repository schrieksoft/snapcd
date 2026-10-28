// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.RunnerNamespaceSupplies;
using SnapCd.Server.Core.Entities.Definition.RunnerSupplies;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RunnerSupplies;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RunnerSupplies;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Views;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Services.Crud;

public class RunnerNamespaceSupplyService : GenericCrudService<
    RunnerNamespaceSupply, RunnerNamespaceSupplyMetadata,
    RunnerNamespaceSupplyCreateDto,
    RunnerNamespaceSupplyUpdateDto,
    RunnerNamespaceSupplyReadDto,
    RunnerNamespaceSupplyMetadataReadDto,
    RunnerNamespaceSupplySecuredRepository,
    RunnerNamespaceSupplyRepository,
    RunnerNamespaceSupplyCreatedEvent,
    RunnerNamespaceSupplyUpdatedEvent,
    RunnerNamespaceSupplyDeletedEvent,
    RunnerNamespaceSupplyRepositorySettings>
{
    public RunnerNamespaceSupplyService(
        RunnerNamespaceSupplySecuredRepository securedRepository) : base(securedRepository)
    {
    }


    protected override RunnerNamespaceSupply MapToEntity(RunnerNamespaceSupplyCreateDto dto, Guid organizationId)
    {
        return RunnerNamespaceSupplyMapper.ToEntity(dto, organizationId);
    }

    protected override RunnerNamespaceSupplyReadDto MapToDto(RunnerNamespaceSupply entity)
    {
        return RunnerNamespaceSupplyMapper.ToDto(entity);
    }

    protected override RunnerNamespaceSupplyMetadataReadDto MapToMetadataDto(RunnerNamespaceSupplyMetadata view)
    {
        return RunnerNamespaceSupplyMapper.ToMetadataDto(view);
    }

    protected override void UpdateEntityFromDto(RunnerNamespaceSupply entity, RunnerNamespaceSupplyUpdateDto dto)
    {
        RunnerNamespaceSupplyMapper.UpdateEntity(entity, dto);
    }
}