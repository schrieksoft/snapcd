using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Filters;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.Crud;

namespace SnapCd.Server.Core.Controllers.Crud;

public static class OrganizationUserCustomEndpointNames
{
    public const string GetByUsername = "ByUsername";
}

[Route(ControllerEndpoints.User)]
[ApiController]
[Authorize("BearerPolicy")]
[OrganizationScopedIAM]
public class OrganizationUserController : ControllerBase
{
    protected readonly OrganizationUserService Service;

    public OrganizationUserController(OrganizationUserService service)
    {
        Service = service;
    }

    [HttpGet]
    public virtual async Task<ActionResult<List<UserViewDto>>> List(Guid organizationId)
    {
        try
        {
            var users = await Service.List(organizationId);
            return Ok(users);
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

    [HttpGet($"{OrganizationUserCustomEndpointNames.GetByUsername}/{{username}}")]
    public virtual async Task<ActionResult<UserViewDto>> GetByUsername(Guid organizationId, string username)
    {
        try
        {
            var user = await Service.GetByUsername(username, organizationId);
            return Ok(user);
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
}