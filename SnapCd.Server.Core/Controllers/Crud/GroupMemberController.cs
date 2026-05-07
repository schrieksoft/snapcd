using EntityFramework.Exceptions.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.GroupMembers.Base;
using SnapCd.Server.Core.Filters;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.Crud;

namespace SnapCd.Server.Core.Controllers.Crud;

public static class GroupMemberCustomEndpointNames
{
    public const string GetByName = "ByName";
}

[Route(ControllerEndpoints.GroupMember)]
[ApiController]
[Authorize("BearerPolicy")]
[OrganizationScopedIAM]
public class GroupMemberController : ControllerBase
{
    protected readonly GroupMemberService Service;

    public GroupMemberController(GroupMemberService service)
    {
        Service = service;
    }

    [HttpPost]
    public virtual async Task<ActionResult<GroupMemberReadDto>> Create(Guid organizationId, GroupMemberReadDto dto)
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
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }

    [HttpGet]
    public virtual async Task<ActionResult<List<GroupMemberReadDto>>> List(Guid organizationId)
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
    public virtual async Task<ActionResult<GroupMemberReadDto>> Get(Guid organizationId, Guid id)
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
    public virtual async Task<ActionResult<GroupMemberReadDto>> Update(Guid organizationId, GroupMemberUpdateDto dto, Guid id)
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