// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.AgentModuleSupplies;
using SnapCd.Server.Core.Controllers.Crud.Generic;
using SnapCd.Server.Core.Entities.Definition.AgentSupplies;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.AgentSupplies;
using SnapCd.Server.Core.Repositories.Organizations.Secured.AgentSupplies;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Controllers.Crud;

public static class AgentModuleSupplyCustomEndpointNames
{
    public const string GetByName = "ByName";
}

[Route(ControllerEndpoints.AgentModuleSupply)]
public class AgentModuleSupplyController : GenericCrudController<
    AgentModuleSupply,
    AgentModuleSupplyCreateDto,
    AgentModuleSupplyUpdateDto,
    AgentModuleSupplyReadDto,
    AgentModuleSupplySecuredRepository,
    AgentModuleSupplyRepository,
    AgentModuleSupplyService,
    AgentModuleSupplyCreatedEvent,
    AgentModuleSupplyUpdatedEvent,
    AgentModuleSupplyDeletedEvent,
    AgentModuleSupplyRepositorySettings>
{
    public AgentModuleSupplyController(AgentModuleSupplyService service) : base(service)
    {
    }
}
