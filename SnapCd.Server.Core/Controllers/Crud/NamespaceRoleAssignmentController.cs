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
using SnapCd.Contracts.Dto.RoleAssignments.Base;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.Crud.RoleAssignment;

namespace SnapCd.Server.Core.Controllers.Crud;

[Route(ControllerEndpoints.NamespaceRoleAssignment)]
[ApiController]
[Authorize("BearerPolicy")]
public class NamespaceRoleAssignmentController : ControllerBase
{
    protected readonly NamespaceRoleAssignmentService Service;

    public NamespaceRoleAssignmentController(NamespaceRoleAssignmentService service)
    {
        Service = service;
    }

    [HttpPost]
    public virtual async Task<ActionResult<NamespaceRoleAssignmentReadDto>> Create(Guid organizationId, NamespaceRoleAssignmentReadDto dto)
    {
        try
        {
            var createdEntity = await Service.Create(dto, organizationId);
            return Ok(createdEntity);
        }
        catch (UniqueConstraintException e)
        {
            return StatusCode(CustomStatusCodes.Status442EntityAlreadyExists, e.Message);
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
        catch (ArgumentException e)
        {
            return StatusCode(StatusCodes.Status400BadRequest, e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [HttpGet]
    public virtual async Task<ActionResult<List<NamespaceRoleAssignmentReadDto>>> List(Guid organizationId)
    {
        try
        {
            var entities = await Service.List(organizationId);
            return Ok(entities);
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

    [HttpGet("{id}")]
    public virtual async Task<ActionResult<NamespaceRoleAssignmentReadDto>> Get(Guid organizationId, Guid id)
    {
        try
        {
            var entity = await Service.Get(id, organizationId);
            return Ok(entity);
        }
        catch (EntityNotFoundException e)
        {
            return StatusCode(CustomStatusCodes.Status441EntityNotFound, e.Message);
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

    [HttpPut("{id}")]
    public virtual async Task<ActionResult<NamespaceRoleAssignmentReadDto>> Update(Guid organizationId, NamespaceRoleAssignmentUpdateDto dto, Guid id)
    {
        try
        {
            var updatedEntity = await Service.Update(dto, id, organizationId);
            return Ok(updatedEntity);
        }
        catch (EntityNotFoundException e)
        {
            return StatusCode(CustomStatusCodes.Status441EntityNotFound, e.Message);
        }
        catch (UniqueConstraintException e)
        {
            return StatusCode(CustomStatusCodes.Status442EntityAlreadyExists, e.Message);
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
        catch (InvalidOperationException e)
        {
            return StatusCode(StatusCodes.Status400BadRequest, e.Message);
        }
        catch (ArgumentException e)
        {
            return StatusCode(StatusCodes.Status400BadRequest, e.Message);
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [HttpDelete("{id}")]
    public virtual async Task<IActionResult> Delete(Guid organizationId, Guid id)
    {
        try
        {
            await Service.Delete(id, organizationId);
            return NoContent();
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