// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.RunnerStackSupplies;
using SnapCd.Server.Core.Controllers.Crud.Generic;
using SnapCd.Server.Core.Entities.Definition.RunnerSupplies;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RunnerSupplies;
using SnapCd.Server.Core.Repositories.Organizations.Secured.RunnerSupplies;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Controllers.Crud;

public static class RunnerStackSupplyCustomEndpointNames
{
    public const string GetByName = "ByName";
}

[Route(ControllerEndpoints.RunnerStackSupply)]
public class RunnerStackSupplyController : GenericCrudController<
    RunnerStackSupply,
    RunnerStackSupplyCreateDto,
    RunnerStackSupplyUpdateDto,
    RunnerStackSupplyReadDto,
    RunnerStackSupplySecuredRepository,
    RunnerStackSupplyRepository,
    RunnerStackSupplyService,
    RunnerStackSupplyCreatedEvent,
    RunnerStackSupplyUpdatedEvent,
    RunnerStackSupplyDeletedEvent,
    RunnerStackSupplyRepositorySettings>
{
    public RunnerStackSupplyController(RunnerStackSupplyService service) : base(service)
    {
    }
}