using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.DependsOnModules;
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

[Route(ControllerEndpoints.DependsOnModule)]
public class DependsOnModuleController : GenericCrudController<
    DependsOnModule,
    DependsOnModuleCreateDto,
    DependsOnModuleUpdateDto,
    DependsOnModuleReadDto,
    DependsOnModuleSecuredRepository,
    DependsOnModuleRepository,
    DependsOnModuleService,
    DependsOnModuleCreatedEvent,
    DependsOnModuleUpdatedEvent,
    DependsOnModuleDeletedEvent,
    DependsOnModuleRepositorySettings>
{
    public DependsOnModuleController(DependsOnModuleService service) : base(service)
    {
    }

    [HttpGet("{moduleId}/{dependsOnModuleId}")]
    public async Task<ActionResult<DependsOnModuleReadDto>> Get(Guid organizationId, Guid moduleId, Guid dependsOnModuleId)
    {
        try
        {
            var dependsOnModuleDto = await Service.Get(moduleId, dependsOnModuleId, organizationId);
            return Ok(dependsOnModuleDto);
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