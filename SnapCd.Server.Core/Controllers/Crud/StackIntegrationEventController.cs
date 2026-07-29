// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.IntegrationEvents;
using SnapCd.Contracts.Mcp;
using SnapCd.Server.Core.Controllers.Crud.Generic;
using SnapCd.Server.Core.Entities.Definition.IntegrationEvents;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Controllers.Crud;

[Route(ControllerEndpoints.StackIntegrationEvent)]
[McpEntity(Singular = "StackIntegrationEvent", Plural = "StackIntegrationEvents")]
public class StackIntegrationEventController : GenericCrudController<
    StackIntegrationEvent,
    StackIntegrationEventCreateDto,
    StackIntegrationEventUpdateDto,
    StackIntegrationEventReadDto,
    StackIntegrationEventSecuredRepository,
    StackIntegrationEventRepository,
    StackIntegrationEventService,
    StackIntegrationEventCreatedEvent,
    StackIntegrationEventUpdatedEvent,
    StackIntegrationEventDeletedEvent,
    StackIntegrationEventRepositorySettings>
{
    public StackIntegrationEventController(StackIntegrationEventService service) : base(service)
    {
    }

    [HttpPost]
    [ExposeAsMcpTool]
    public override Task<ActionResult<StackIntegrationEventReadDto>> Create(Guid organizationId, StackIntegrationEventCreateDto dto)
        => base.Create(organizationId, dto);

    [HttpGet("ByIntegration/{integrationId}")]
    [ExposeAsMcpTool]
    public async Task<ActionResult<List<StackIntegrationEventReadDto>>> ListByIntegration(Guid organizationId, Guid integrationId)
    {
        try
        {
            var events = await Service.ListByIntegration(integrationId, organizationId);
            return Ok(events);
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

    [HttpGet("ByStack/{stackId}")]
    [ExposeAsMcpTool]
    public async Task<ActionResult<List<StackIntegrationEventReadDto>>> ListByStack(Guid organizationId, Guid stackId)
    {
        try
        {
            var events = await Service.ListByStack(stackId, organizationId);
            return Ok(events);
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
