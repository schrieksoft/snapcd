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

[Route(ControllerEndpoints.NamespaceMission)]
[McpEntity(Singular = "Namespace Mission", Plural = "Namespace Missions")]
public class NamespaceMissionController : GenericCrudController<
    NamespaceMission,
    NamespaceMissionCreateDto,
    NamespaceMissionUpdateDto,
    NamespaceMissionReadDto,
    NamespaceMissionSecuredRepository,
    NamespaceMissionRepository,
    NamespaceMissionService,
    NamespaceMissionCreatedEvent,
    NamespaceMissionUpdatedEvent,
    NamespaceMissionDeletedEvent,
    NamespaceMissionRepositorySettings>
{
    public NamespaceMissionController(NamespaceMissionService service) : base(service)
    {
    }

    /// <summary>Create a new Namespace Mission. Fires for events on modules within the targeted Namespace.</summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="dto">The Namespace Mission to create</param>
    [HttpPost]
    [ExposeAsMcpTool]
    public override Task<ActionResult<NamespaceMissionReadDto>> Create(Guid organizationId, NamespaceMissionCreateDto dto)
        => base.Create(organizationId, dto);

    /// <summary>List Namespace Missions assigned to a specific Agent.</summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="agentId">Agent ID</param>
    [HttpGet("ByAgent/{agentId}")]
    [ExposeAsMcpTool]
    public async Task<ActionResult<List<NamespaceMissionReadDto>>> ListByAgent(Guid organizationId, Guid agentId)
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

    /// <summary>List Namespace Missions targeting a specific Namespace.</summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="namespaceId">Namespace ID</param>
    [HttpGet("ByNamespace/{namespaceId}")]
    [ExposeAsMcpTool]
    public async Task<ActionResult<List<NamespaceMissionReadDto>>> ListByNamespace(Guid organizationId, Guid namespaceId)
    {
        try
        {
            var missions = await Service.ListByNamespace(namespaceId, organizationId);
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
