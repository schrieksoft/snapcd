// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.ModuleHolds;
using SnapCd.Server.Core.Filters;
using SnapCd.Server.Core.Misc.Attributes;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.ModuleHold;

namespace SnapCd.Server.Core.Controllers.ModuleHolds;

/// <summary>
/// Lets a client that is configuring modules (the provider, during an apply) hold them out of deployment until it has
/// finished. A hold lasts as long as the client keeps sending heartbeats for it.
/// </summary>
[Route(ControllerEndpoints.ModuleHold)]
[ApiController]
[Authorize("BearerPolicy")]
[OrganizationScopedFeature]
public class ModuleHoldController : ControllerBase
{
    private readonly ModuleHoldService _service;

    public ModuleHoldController(ModuleHoldService service)
    {
        _service = service;
    }

    [EndpointSummary("Hold the listed Modules out of deployment, or keep holding them")]
    [PermissionSource(Repository = typeof(ModuleSecuredRepository), Verb = PermissionVerb.Update)]
    [HttpPost("heartbeat")]
    public async Task<ActionResult<ModuleHoldHeartbeatResponseDto>> Heartbeat(Guid organizationId, [FromBody] ModuleHoldHeartbeatRequestDto request)
    {
        try
        {
            return Ok(await _service.Heartbeat(organizationId, request));
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [EndpointSummary("Release the listed Modules the caller holds")]
    [PermissionSource(Repository = typeof(ModuleSecuredRepository), Verb = PermissionVerb.Update)]
    [HttpPost("release")]
    public async Task<ActionResult> Release(Guid organizationId, [FromBody] ModuleHoldReleaseRequestDto request)
    {
        try
        {
            await _service.Release(organizationId, request);
            return NoContent();
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }
}
