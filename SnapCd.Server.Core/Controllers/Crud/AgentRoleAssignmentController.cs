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
using SnapCd.Contracts.Mcp;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.Crud.RoleAssignment;

namespace SnapCd.Server.Core.Controllers.Crud;

[Route(ControllerEndpoints.AgentRoleAssignment)]
[ApiController]
[Authorize("BearerPolicy")]
[McpEntity(Singular = "Agent Role Assignment", Plural = "Agent Role Assignments")]
public class AgentRoleAssignmentController : ControllerBase
{
    protected readonly AgentRoleAssignmentService Service;

    public AgentRoleAssignmentController(AgentRoleAssignmentService service)
    {
        Service = service;
    }

    /// <summary>Create a new Agent Role Assignment. PrincipalDiscriminator selects the variant (User / ServicePrincipal / Group).</summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="dto">The Agent Role Assignment to create</param>
    [HttpPost]
    [ExposeAsMcpTool]
    public virtual async Task<ActionResult<AgentRoleAssignmentReadDto>> Create(Guid organizationId, AgentRoleAssignmentReadDto dto)
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

    /// <summary>List all Agent Role Assignments in the organization.</summary>
    /// <param name="organizationId">Organization ID</param>
    [HttpGet]
    [ExposeAsMcpTool]
    public virtual async Task<ActionResult<List<AgentRoleAssignmentReadDto>>> List(Guid organizationId)
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

    /// <summary>Get a single Agent Role Assignment by ID.</summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="id">Agent Role Assignment ID</param>
    [HttpGet("{id}")]
    [ExposeAsMcpTool]
    public virtual async Task<ActionResult<AgentRoleAssignmentReadDto>> Get(Guid organizationId, Guid id)
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

    /// <summary>Update an existing Agent Role Assignment. PrincipalDiscriminator cannot be changed; delete and recreate to switch principal type.</summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="dto">The new Agent Role Assignment values</param>
    /// <param name="id">Agent Role Assignment ID</param>
    [HttpPut("{id}")]
    [ExposeAsMcpTool]
    public virtual async Task<ActionResult<AgentRoleAssignmentReadDto>> Update(Guid organizationId, AgentRoleAssignmentUpdateDto dto, Guid id)
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

    /// <summary>Delete the Agent Role Assignment.</summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="id">Agent Role Assignment ID</param>
    [HttpDelete("{id}")]
    [ExposeAsMcpTool]
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
