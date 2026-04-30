using EntityFramework.Exceptions.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.RoleAssignments.Base;
using SnapCd.Server.Core.Licensing.Attributes;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.Crud.RoleAssignment;

namespace SnapCd.Server.Core.Controllers.Crud;

[Route(ControllerEndpoints.ModuleRoleAssignment)]
[ApiController]
[Authorize("BearerPolicy")]
public class ModuleRoleAssignmentController : ControllerBase
{
    protected readonly ModuleRoleAssignmentService Service;

    public ModuleRoleAssignmentController(ModuleRoleAssignmentService service)
    {
        Service = service;
    }

    [HttpPost]
    [VerifyLicense("FinegrainedRbac")]
    public virtual async Task<ActionResult<ModuleRoleAssignmentReadDto>> Create(Guid organizationId, ModuleRoleAssignmentReadDto dto)
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
    public virtual async Task<ActionResult<List<ModuleRoleAssignmentReadDto>>> List(Guid organizationId)
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
    public virtual async Task<ActionResult<ModuleRoleAssignmentReadDto>> Get(Guid organizationId, Guid id)
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
    [VerifyLicense("FinegrainedRbac")]
    public virtual async Task<ActionResult<ModuleRoleAssignmentReadDto>> Update(Guid organizationId, ModuleRoleAssignmentUpdateDto dto, Guid id)
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