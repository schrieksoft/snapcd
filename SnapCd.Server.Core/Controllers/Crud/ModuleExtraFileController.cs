using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.ModuleExtraFiles;
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

[Route(ControllerEndpoints.ModuleExtraFile)]
public class ModuleExtraFileController : GenericCrudController<
    ModuleExtraFile,
    ModuleExtraFileCreateDto,
    ModuleExtraFileUpdateDto,
    ModuleExtraFileReadDto,
    ModuleExtraFileSecuredRepository,
    ModuleExtraFileRepository,
    ModuleExtraFileService,
    ModuleExtraFileCreatedEvent,
    ModuleExtraFileUpdatedEvent,
    ModuleExtraFileDeletedEvent,
    ModuleExtraFileRepositorySettings>
{
    public ModuleExtraFileController(ModuleExtraFileService service) : base(service)
    {
    }

    [HttpGet("{moduleId}/{name}")]
    public async Task<ActionResult<ModuleExtraFileReadDto>> Get(Guid organizationId, Guid moduleId, string name)
    {
        try
        {
            var moduleExtraFileDto = await Service.Get(moduleId, name, organizationId);
            return Ok(moduleExtraFileDto);
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