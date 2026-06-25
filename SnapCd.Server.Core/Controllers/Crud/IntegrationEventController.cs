// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.Integrations;
using SnapCd.Server.Core.Filters;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.Integrations;

namespace SnapCd.Server.Core.Controllers.Crud;

[Route(ControllerEndpoints.IntegrationEvent)]
[ApiController]
[Authorize("BearerPolicy")]
[OrganizationScopedFeature]
public class IntegrationEventController : ControllerBase
{
    private readonly IntegrationEventService _service;

    public IntegrationEventController(IntegrationEventService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<IntegrationEventDto>>> List(Guid organizationId)
        => await _service.List(organizationId);

    [HttpGet("{scope}/{id}")]
    public async Task<ActionResult<IntegrationEventDto>> Get(Guid organizationId, IntegrationEventScope scope, Guid id)
    {
        try
        {
            return await _service.GetOne(organizationId, scope, id);
        }
        catch (EntityNotFoundException e) { return StatusCode(Misc.Constants.CustomStatusCodes.Status441EntityNotFound, e.Message); }
    }

    [HttpPost]
    public async Task<ActionResult<IntegrationEventDto>> Create(Guid organizationId, [FromBody] IntegrationEventCreateDto dto)
    {
        try
        {
            var id = await _service.Create(organizationId, dto);
            return await _service.GetOne(organizationId, dto.Scope, id);
        }
        catch (PrincipalNotAuthorizedException e) { return StatusCode(StatusCodes.Status403Forbidden, e.Message); }
        catch (ArgumentException e) { return BadRequest(e.Message); }
        catch (DbUpdateException) { return StatusCode(Misc.Constants.CustomStatusCodes.Status442EntityAlreadyExists, "An event with this trigger already exists for that integration on that scope."); }
        catch (Exception e) { return StatusCode(StatusCodes.Status500InternalServerError, e.Message); }
    }

    [HttpPut("{scope}/{id}")]
    public async Task<IActionResult> Update(Guid organizationId, IntegrationEventScope scope, Guid id, [FromBody] IntegrationEventUpdateDto dto)
    {
        try
        {
            await _service.Update(organizationId, scope, id, dto);
            return NoContent();
        }
        catch (PrincipalNotAuthorizedException e) { return StatusCode(StatusCodes.Status403Forbidden, e.Message); }
        catch (EntityNotFoundException e) { return StatusCode(Misc.Constants.CustomStatusCodes.Status441EntityNotFound, e.Message); }
        catch (DbUpdateException) { return StatusCode(Misc.Constants.CustomStatusCodes.Status442EntityAlreadyExists, "An event with this trigger already exists for that integration on that scope."); }
        catch (Exception e) { return StatusCode(StatusCodes.Status500InternalServerError, e.Message); }
    }

    [HttpDelete("{scope}/{id}")]
    public async Task<IActionResult> Delete(Guid organizationId, IntegrationEventScope scope, Guid id)
    {
        try
        {
            await _service.Delete(organizationId, scope, id);
            return NoContent();
        }
        catch (PrincipalNotAuthorizedException e) { return StatusCode(StatusCodes.Status403Forbidden, e.Message); }
        catch (EntityNotFoundException e) { return StatusCode(Misc.Constants.CustomStatusCodes.Status441EntityNotFound, e.Message); }
        catch (Exception e) { return StatusCode(StatusCodes.Status500InternalServerError, e.Message); }
    }
}
