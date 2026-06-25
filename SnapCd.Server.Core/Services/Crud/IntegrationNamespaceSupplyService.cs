// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Dto.IntegrationNamespaceSupplies;
using SnapCd.Server.Core.Entities.Definition.IntegrationSupplies;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured.IntegrationSupplies;
using SnapCd.Server.Core.Services.Crud.Generic;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Services.Crud;

public class IntegrationNamespaceSupplyService : GenericCrudService<
    IntegrationNamespaceSupply,
    IntegrationNamespaceSupplyCreateDto,
    IntegrationNamespaceSupplyUpdateDto,
    IntegrationNamespaceSupplyReadDto,
    IntegrationNamespaceSupplySecuredRepository,
    IntegrationNamespaceSupplyRepository,
    IntegrationNamespaceSupplyCreatedEvent,
    IntegrationNamespaceSupplyUpdatedEvent,
    IntegrationNamespaceSupplyDeletedEvent,
    IntegrationNamespaceSupplyRepositorySettings>
{
    public IntegrationNamespaceSupplyService(
        IntegrationNamespaceSupplySecuredRepository securedRepository) : base(securedRepository)
    {
    }

    protected override IntegrationNamespaceSupply MapToEntity(IntegrationNamespaceSupplyCreateDto dto, Guid organizationId)
    {
        return IntegrationNamespaceSupplyMapper.ToEntity(dto, organizationId);
    }

    protected override IntegrationNamespaceSupplyReadDto MapToDto(IntegrationNamespaceSupply entity)
    {
        return IntegrationNamespaceSupplyMapper.ToDto(entity);
    }

    protected override void UpdateEntityFromDto(IntegrationNamespaceSupply entity, IntegrationNamespaceSupplyUpdateDto dto)
    {
        IntegrationNamespaceSupplyMapper.UpdateEntity(entity, dto);
    }
}
