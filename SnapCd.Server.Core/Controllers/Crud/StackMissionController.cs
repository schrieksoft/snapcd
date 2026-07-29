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

[Route(ControllerEndpoints.StackMission)]
[McpEntity(Singular = "StackMission", Plural = "StackMissions")]
public class StackMissionController : GenericCrudController<
    StackMission,
    StackMissionCreateDto,
    StackMissionUpdateDto,
    StackMissionReadDto,
    StackMissionSecuredRepository,
    StackMissionRepository,
    StackMissionService,
    StackMissionCreatedEvent,
    StackMissionUpdatedEvent,
    StackMissionDeletedEvent,
    StackMissionRepositorySettings>
{
    public StackMissionController(StackMissionService service) : base(service)
    {
    }

    [HttpPost]
    [ExposeAsMcpTool(Instructions = "Fires for events on modules within the targeted Stack.")]
    public override Task<ActionResult<StackMissionReadDto>> Create(Guid organizationId, StackMissionCreateDto dto)
        => base.Create(organizationId, dto);

    [EndpointSummary("List StackMissions assigned to a specific Agent")]
    [HttpGet("ByAgent/{agentId}")]
    [ExposeAsMcpTool]
    public async Task<ActionResult<List<StackMissionReadDto>>> ListByAgent(Guid organizationId, Guid agentId)
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

    [EndpointSummary("List StackMissions targeting a specific Stack")]
    [HttpGet("ByStack/{stackId}")]
    [ExposeAsMcpTool]
    public async Task<ActionResult<List<StackMissionReadDto>>> ListByStack(Guid organizationId, Guid stackId)
    {
        try
        {
            var missions = await Service.ListByStack(stackId, organizationId);
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
