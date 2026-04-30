using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.NamespaceExtraFiles;
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

[Route(ControllerEndpoints.NamespaceExtraFile)]
public class NamespaceExtraFileController : GenericCrudController<
    NamespaceExtraFile,
    NamespaceExtraFileCreateDto,
    NamespaceExtraFileUpdateDto,
    NamespaceExtraFileReadDto,
    NamespaceExtraFileSecuredRepository,
    NamespaceExtraFileRepository,
    NamespaceExtraFileService,
    NamespaceExtraFileCreatedEvent,
    NamespaceExtraFileUpdatedEvent,
    NamespaceExtraFileDeletedEvent,
    NamespaceExtraFileRepositorySettings>
{
    public NamespaceExtraFileController(NamespaceExtraFileService service) : base(service)
    {
    }

    [HttpGet("{namespaceId}/{name}")]
    public async Task<ActionResult<NamespaceExtraFileReadDto>> Get(Guid organizationId, Guid namespaceId, string name)
    {
        try
        {
            var namespaceExtraFileDto = await Service.Get(namespaceId, name, organizationId);
            return Ok(namespaceExtraFileDto);
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