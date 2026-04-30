using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.Stacks;
using SnapCd.Server.Core.Controllers.Crud.Generic;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Controllers.Crud;

public static class StackCustomEndpointNames
{
    public const string GetStackByName = "ByName";
}

[Route(ControllerEndpoints.Stack)]
[ApiController]
[Authorize("BearerPolicy")]
public class StackController : GenericCrudController<
    Stack,
    StackCreateDto,
    StackUpdateDto,
    StackReadDto,
    StackSecuredRepository,
    StackRepository,
    StackService,
    StackCreatedEvent,
    StackUpdatedEvent,
    StackDeletedEvent,
    StackRepositorySettings>
{
    public StackController(StackService service) : base(service)
    {
    }

    [HttpGet($"{StackCustomEndpointNames.GetStackByName}/{{name}}")]
    public async Task<ActionResult<StackReadDto>> GetByName(Guid organizationId, string name)
    {
        try
        {
            var stackDto = await Service.GetByName(name, organizationId);
            return Ok(stackDto);
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