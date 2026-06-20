// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.AgentStackSupplies;
using SnapCd.Server.Core.Entities.Definition.AgentSupplies;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.AgentSupplies;
using SnapCd.Server.Core.Repositories.Organizations.Secured.AgentSupplies;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class AgentStackSupplyService : GenericCrudService<
    AgentStackSupply,
    AgentStackSupplyCreateDto,
    AgentStackSupplyUpdateDto,
    AgentStackSupplyReadDto,
    AgentStackSupplySecuredRepository,
    AgentStackSupplyRepository,
    AgentStackSupplyCreatedEvent,
    AgentStackSupplyUpdatedEvent,
    AgentStackSupplyDeletedEvent,
    AgentStackSupplyRepositorySettings>
{
    public AgentStackSupplyService(
        AgentStackSupplySecuredRepository securedRepository) : base(securedRepository)
    {
    }


    protected override AgentStackSupply MapToEntity(AgentStackSupplyCreateDto dto, Guid organizationId)
    {
        return AgentStackSupplyMapper.ToEntity(dto, organizationId);
    }

    protected override AgentStackSupplyReadDto MapToDto(AgentStackSupply entity)
    {
        return AgentStackSupplyMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(AgentStackSupply entity, AgentStackSupplyUpdateDto dto)
    {
        AgentStackSupplyMapper.UpdateEntity(entity, dto);
    }
}
