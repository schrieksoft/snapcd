// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.RunnerModuleSupplies;
using SnapCd.Server.Core.Entities.Definition.RunnerSupplies;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RunnerSupplies;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RunnerSupplies;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class RunnerModuleSupplyService : GenericCrudService<
    RunnerModuleSupply,
    RunnerModuleSupplyCreateDto,
    RunnerModuleSupplyUpdateDto,
    RunnerModuleSupplyReadDto,
    RunnerModuleSupplySecuredRepository,
    RunnerModuleSupplyRepository,
    RunnerModuleSupplyCreatedEvent,
    RunnerModuleSupplyUpdatedEvent,
    RunnerModuleSupplyDeletedEvent,
    RunnerModuleSupplyRepositorySettings>
{
    public RunnerModuleSupplyService(
        RunnerModuleSupplySecuredRepository securedRepository
    ) : base(securedRepository)
    {
    }

    protected override RunnerModuleSupply MapToEntity(RunnerModuleSupplyCreateDto dto, Guid organizationId)
    {
        return RunnerModuleSupplyMapper.ToEntity(dto, organizationId);
    }

    protected override RunnerModuleSupplyReadDto MapToDto(RunnerModuleSupply entity)
    {
        return RunnerModuleSupplyMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(RunnerModuleSupply entity, RunnerModuleSupplyUpdateDto dto)
    {
        RunnerModuleSupplyMapper.UpdateEntity(entity, dto);
    }
}