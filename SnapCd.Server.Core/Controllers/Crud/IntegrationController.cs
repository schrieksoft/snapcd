// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Server.Core.Misc.Attributes;
using System.ComponentModel.DataAnnotations;
using EntityFramework.Exceptions.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.Integrations;
using SnapCd.Contracts.Mcp;
using SnapCd.Server.Core.Filters;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.Integrations;
using SnapCd.Server.Core.Services.Integrations.Codecs;

namespace SnapCd.Server.Core.Controllers.Crud;

[Route(ControllerEndpoints.Integration)]
[ApiController]
[Authorize("BearerPolicy")]
[OrganizationScopedFeature]
[McpEntity(Singular = "Integration", Plural = "Integrations")]
public class IntegrationController : ControllerBase
{
    private readonly IntegrationService _service;
    private readonly IntegrationSupplyService _supplies;

    public IntegrationController(IntegrationService service, IntegrationSupplyService supplies)
    {
        _service = service;
        _supplies = supplies;
    }

    [HttpGet]
    public async Task<ActionResult<List<IntegrationReadDto>>> List(Guid organizationId)
        => await _service.List(organizationId);

    [HttpGet("ByName/{name}")]
    public async Task<ActionResult<IntegrationReadDto>> GetByName(Guid organizationId, string name)
    {
        try
        {
            return await _service.GetReadByName(name, organizationId);
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
        catch (EntityNotFoundException e)
        {
            return StatusCode(Misc.Constants.CustomStatusCodes.Status441EntityNotFound, e.Message);
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<IntegrationReadDto>> Get(Guid organizationId, Guid id)
    {
        try
        {
            return await _service.GetRead(id, organizationId);
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
        catch (EntityNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [HttpPost]
    public async Task<ActionResult<IntegrationReadDto>> Create(Guid organizationId, [FromBody] IntegrationCreateDto dto)
    {
        try
        {
            var id = await _service.Create(organizationId, dto);
            return await _service.GetRead(id, organizationId);
        }
        catch (UniqueConstraintException e)
        {
            return StatusCode(CustomStatusCodes.Status442EntityAlreadyExists, e.Message);
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
        catch (ValidationException e)
        {
            return BadRequest(e.Message);
        }
        catch (DbUpdateException)
        {
            return StatusCode(Misc.Constants.CustomStatusCodes.Status442EntityAlreadyExists, "An integration with this name and type already exists.");
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<IntegrationReadDto>> Update(Guid organizationId, Guid id, [FromBody] IntegrationUpdateDto dto)
    {
        try
        {
            await _service.Update(id, organizationId, dto);
            return await _service.GetRead(id, organizationId);
        }
        catch (UniqueConstraintException e)
        {
            return StatusCode(CustomStatusCodes.Status442EntityAlreadyExists, e.Message);
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
        catch (EntityNotFoundException e)
        {
            return StatusCode(Misc.Constants.CustomStatusCodes.Status441EntityNotFound, e.Message);
        }
        catch (ValidationException e)
        {
            return BadRequest(e.Message);
        }
        catch (DbUpdateException)
        {
            return StatusCode(Misc.Constants.CustomStatusCodes.Status442EntityAlreadyExists, "An integration with this name and type already exists.");
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid organizationId, Guid id)
    {
        try
        {
            await _service.Delete(id, organizationId);
            return NoContent();
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
        catch (EntityNotFoundException e)
        {
            return StatusCode(Misc.Constants.CustomStatusCodes.Status441EntityNotFound, e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [HttpPost("{id}/test")]
    [PermissionSource(Skip = true)]
    public async Task<ActionResult<IntegrationTestResult>> Test(Guid organizationId, Guid id)
    {
        try
        {
            return await _service.TestConnection(id, organizationId);
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
        catch (EntityNotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    // ---- Supplies ----

    [HttpGet("{id}/supplies")]
    public async Task<ActionResult<List<IntegrationSupplyDto>>> ListSupplies(Guid organizationId, Guid id)
    {
        try
        {
            return await _supplies.List(id, organizationId);
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
    }

    [HttpGet("{id}/supplies/{scope}/{supplyId}")]
    public async Task<ActionResult<IntegrationSupplyDto>> GetSupply(Guid organizationId, Guid id, IntegrationSupplyScope scope, Guid supplyId)
    {
        try
        {
            return await _supplies.GetOne(id, organizationId, scope, supplyId);
        }
        catch (EntityNotFoundException e)
        {
            return StatusCode(Misc.Constants.CustomStatusCodes.Status441EntityNotFound, e.Message);
        }
    }

    [HttpPost("{id}/supplies")]
    public async Task<ActionResult<IntegrationSupplyDto>> AddSupply(Guid organizationId, Guid id, [FromBody] IntegrationSupplyCreateDto dto)
    {
        try
        {
            var supplyId = await _supplies.Add(id, organizationId, dto);
            return new IntegrationSupplyDto { Id = supplyId, Scope = dto.Scope, ScopeId = dto.ScopeId };
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
        catch (EntityNotFoundException e)
        {
            return StatusCode(Misc.Constants.CustomStatusCodes.Status441EntityNotFound, e.Message);
        }
        catch (DbUpdateException)
        {
            return StatusCode(Misc.Constants.CustomStatusCodes.Status442EntityAlreadyExists, "This integration is already assigned to that scope.");
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [HttpDelete("{id}/supplies/{scope}/{supplyId}")]
    public async Task<IActionResult> RemoveSupply(Guid organizationId, Guid id, IntegrationSupplyScope scope, Guid supplyId)
    {
        try
        {
            await _supplies.Remove(organizationId, scope, supplyId);
            return NoContent();
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
        catch (EntityNotFoundException e)
        {
            return StatusCode(Misc.Constants.CustomStatusCodes.Status441EntityNotFound, e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }
}
