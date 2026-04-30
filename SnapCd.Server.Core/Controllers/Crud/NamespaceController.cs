using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.Namespaces;
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

[Route(ControllerEndpoints.Namespace)]
public class NamespaceController : GenericCrudController<
    Namespace,
    NamespaceCreateDto,
    NamespaceUpdateDto,
    NamespaceReadDto,
    NamespaceSecuredRepository,
    NamespaceRepository,
    NamespaceService,
    NamespaceCreatedEvent,
    NamespaceUpdatedEvent,
    NamespaceDeletedEvent,
    NamespaceRepositorySettings>
{
    public NamespaceController(NamespaceService service) : base(service)
    {
    }

    [HttpGet("{stackId}/{name}")]
    public async Task<IActionResult> Get(Guid organizationId, Guid stackId, string name)
    {
        try
        {
            var namespaceDto = await Service.Get(stackId, name, organizationId);
            return Ok(namespaceDto);
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