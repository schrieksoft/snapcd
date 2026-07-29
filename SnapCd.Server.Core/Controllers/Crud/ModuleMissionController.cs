// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.Missions;
using SnapCd.Contracts.Mcp;
using SnapCd.Server.Core.Controllers.Crud.Generic;
using SnapCd.Server.Core.Entities.Definition.Missions;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Controllers.Crud;

[Route(ControllerEndpoints.ModuleMission)]
[McpEntity(Singular = "ModuleMission", Plural = "ModuleMissions")]
public class ModuleMissionController : GenericCrudController<
    ModuleMission,
    ModuleMissionCreateDto,
    ModuleMissionUpdateDto,
    ModuleMissionReadDto,
    ModuleMissionSecuredRepository,
    ModuleMissionRepository,
    ModuleMissionService,
    ModuleMissionCreatedEvent,
    ModuleMissionUpdatedEvent,
    ModuleMissionDeletedEvent,
    ModuleMissionRepositorySettings>
{
    public ModuleMissionController(ModuleMissionService service) : base(service)
    {
    }

    [HttpPost]
    [ExposeAsMcpTool(Instructions = "Fires for events on the targeted Module.")]
    public override Task<ActionResult<ModuleMissionReadDto>> Create(Guid organizationId, ModuleMissionCreateDto dto)
        => base.Create(organizationId, dto);

    [EndpointSummary("List ModuleMissions assigned to a specific Agent")]
    [HttpGet("ByAgent/{agentId}")]
    [ExposeAsMcpTool]
    public async Task<ActionResult<List<ModuleMissionReadDto>>> ListByAgent(Guid organizationId, Guid agentId)
    {
        try
        {
            var missions = await Service.ListByAgent(agentId, organizationId);
            return Ok(missions);
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [EndpointSummary("List ModuleMissions targeting a specific Module")]
    [HttpGet("ByModule/{moduleId}")]
    [ExposeAsMcpTool]
    public async Task<ActionResult<List<ModuleMissionReadDto>>> ListByModule(Guid organizationId, Guid moduleId)
    {
        try
        {
            var missions = await Service.ListByModule(moduleId, organizationId);
            return Ok(missions);
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }
}
