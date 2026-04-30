using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.SourceRefresherPreselections;
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

public static class SourceRefresherPreselectionCustomEndpointNames
{
    public const string GetBySourceUrl = "BySourceUrl";
}

[Route(ControllerEndpoints.SourceRefresherPreselection)]
public class SourceRefresherPreselectionController : GenericCrudController<
    SourceRefresherPreselection,
    SourceRefresherPreselectionCreateDto,
    SourceRefresherPreselectionUpdateDto,
    SourceRefresherPreselectionReadDto,
    SourceRefresherPreselectionSecuredRepository,
    SourceRefresherPreselectionRepository,
    SourceRefresherPreselectionService,
    SourceRefresherPreselectionCreatedEvent,
    SourceRefresherPreselectionUpdatedEvent,
    SourceRefresherPreselectionDeletedEvent,
    SourceRefresherPreselectionRepositorySettings>
{
    public SourceRefresherPreselectionController(SourceRefresherPreselectionService service) : base(service)
    {
    }

    [HttpGet($"{SourceRefresherPreselectionCustomEndpointNames.GetBySourceUrl}/{{name}}")]
    public async Task<ActionResult<SourceRefresherPreselectionReadDto>> GetByName(Guid organizationId, string name)
    {
        try
        {
            var sourceRefresherPreselectionDto = await Service.GetBySourceUrl(name, organizationId);
            return Ok(sourceRefresherPreselectionDto);
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