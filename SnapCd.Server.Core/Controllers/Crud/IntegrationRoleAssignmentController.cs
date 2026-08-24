// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using EntityFramework.Exceptions.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.RoleAssignments;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.Crud.RoleAssignment;

namespace SnapCd.Server.Core.Controllers.Crud;

[Route(ControllerEndpoints.IntegrationRoleAssignment)]
[ApiController]
[Authorize("BearerPolicy")]
public class IntegrationRoleAssignmentController : ControllerBase
{
    private readonly IntegrationRoleAssignmentService _service;

    public IntegrationRoleAssignmentController(IntegrationRoleAssignmentService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<List<IntegrationRoleAssignmentReadDto>>> List(Guid organizationId)
    {
        try { return await _service.List(organizationId); }
        catch (PrincipalNotAuthorizedException e) { return StatusCode(StatusCodes.Status403Forbidden, e.Message); }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<IntegrationRoleAssignmentReadDto>> Get(Guid organizationId, Guid id)
    {
        try { return await _service.Get(id, organizationId); }
        catch (PrincipalNotAuthorizedException e) { return StatusCode(StatusCodes.Status403Forbidden, e.Message); }
        catch (EntityNotFoundException e) { return NotFound(e.Message); }
    }

    [HttpPost]
    public async Task<ActionResult<IntegrationRoleAssignmentReadDto>> Create(Guid organizationId, [FromBody] IntegrationRoleAssignmentReadDto dto)
    {
        try { return await _service.Create(dto, organizationId); }
        catch (UniqueConstraintException e) { return StatusCode(CustomStatusCodes.Status442EntityAlreadyExists, e.Message); }
        catch (PrincipalNotAuthorizedException e) { return StatusCode(StatusCodes.Status403Forbidden, e.Message); }
        catch (Exception e) { return StatusCode(StatusCodes.Status500InternalServerError, e.Message); }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<IntegrationRoleAssignmentReadDto>> Update(Guid organizationId, Guid id, [FromBody] IntegrationRoleAssignmentUpdateDto dto)
    {
        try { return await _service.Update(dto, id, organizationId); }
        catch (UniqueConstraintException e) { return StatusCode(CustomStatusCodes.Status442EntityAlreadyExists, e.Message); }
        catch (PrincipalNotAuthorizedException e) { return StatusCode(StatusCodes.Status403Forbidden, e.Message); }
        catch (EntityNotFoundException e) { return NotFound(e.Message); }
        catch (Exception e) { return StatusCode(StatusCodes.Status500InternalServerError, e.Message); }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(Guid organizationId, Guid id)
    {
        try { await _service.Delete(id, organizationId); return NoContent(); }
        catch (PrincipalNotAuthorizedException e) { return StatusCode(StatusCodes.Status403Forbidden, e.Message); }
        catch (EntityNotFoundException e) { return NotFound(e.Message); }
    }
}
