using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.Runners;
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

public static class RunnerCustomEndpointNames
{
    public const string GetRunnerByName = "ByName";
}

[Route(ControllerEndpoints.Runner)]
public class RunnerController : GenericCrudController<
    Runner,
    RunnerCreateDto,
    RunnerUpdateDto,
    RunnerReadDto,
    RunnerSecuredRepository,
    RunnerRepository,
    RunnerService,
    RunnerCreatedEvent,
    RunnerUpdatedEvent,
    RunnerDeletedEvent,
    RunnerRepositorySettings>
{
    public RunnerController(RunnerService service) : base(service)
    {
    }

    [HttpGet($"{RunnerCustomEndpointNames.GetRunnerByName}/{{name}}")]
    public async Task<ActionResult<RunnerReadDto>> GetByName(Guid organizationId, string name)
    {
        try
        {
            var runnerDto = await Service.GetByName(name, organizationId);
            return Ok(runnerDto);
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