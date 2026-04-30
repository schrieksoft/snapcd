using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.Secrets.Scoped;
using SnapCd.Server.Core.Controllers.Crud.Generic;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Secrets;
using SnapCd.Server.Core.Repositories.Organizations.Secured.Secrets.Scoped;
using SnapCd.Server.Core.Services.Crud.Secrets.Scoped;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Controllers.Crud.Secrets;

public static class StackSecretCustomEndpointNames
{
    public const string GetByName = "ByName";
}

[Route(ControllerEndpoints.StackSecret)]
[ApiController]
[Authorize("BearerPolicy")]
public class StackSecretController : GenericCrudController<
    StackSecret,
    StackSecretDto,
    StackSecretDto,
    StackSecretDto,
    StackSecretSecuredRepository,
    StackSecretRepository,
    StackSecretService,
    StackSecretCreatedEvent, 
    StackSecretUpdatedEvent, 
    StackSecretDeletedEvent, 
    StackSecretRepositorySettings>
{
    public StackSecretController(StackSecretService service) : base(service)
    {
    }

    [HttpGet($"{StackSecretCustomEndpointNames.GetByName}/{{name}}")]
    public async Task<ActionResult<StackSecretDto>> GetByName(Guid organizationId, string name)
    {
        try
        {
            var dto = await Service.GetByName(name, organizationId);
            return Ok(dto);
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