// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.RunnerStackSupplies;
using SnapCd.Server.Core.Entities.Definition.RunnerSupplies;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RunnerSupplies;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RunnerSupplies;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class RunnerStackSupplyService : GenericCrudService<
    RunnerStackSupply,
    RunnerStackSupplyCreateDto,
    RunnerStackSupplyUpdateDto,
    RunnerStackSupplyReadDto,
    RunnerStackSupplySecuredRepository,
    RunnerStackSupplyRepository,
    RunnerStackSupplyCreatedEvent,
    RunnerStackSupplyUpdatedEvent,
    RunnerStackSupplyDeletedEvent,
    RunnerStackSupplyRepositorySettings>
{
    public RunnerStackSupplyService(
        RunnerStackSupplySecuredRepository securedRepository) : base(securedRepository)
    {
    }


    protected override RunnerStackSupply MapToEntity(RunnerStackSupplyCreateDto dto, Guid organizationId)
    {
        return RunnerStackSupplyMapper.ToEntity(dto, organizationId);
    }

    protected override RunnerStackSupplyReadDto MapToDto(RunnerStackSupply entity)
    {
        return RunnerStackSupplyMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(RunnerStackSupply entity, RunnerStackSupplyUpdateDto dto)
    {
        RunnerStackSupplyMapper.UpdateEntity(entity, dto);
    }
}