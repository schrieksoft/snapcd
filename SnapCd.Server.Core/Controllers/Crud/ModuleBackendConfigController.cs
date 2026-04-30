using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.ModuleBackendConfigs;
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

[Route(ControllerEndpoints.ModuleBackendConfig)]
public class ModuleBackendConfigController : GenericCrudController<
    ModuleBackendConfig,
    ModuleBackendConfigCreateDto,
    ModuleBackendConfigUpdateDto,
    ModuleBackendConfigReadDto,
    ModuleBackendConfigSecuredRepository,
    ModuleBackendConfigRepository,
    ModuleBackendConfigService,
    ModuleBackendConfigCreatedEvent,
    ModuleBackendConfigUpdatedEvent,
    ModuleBackendConfigDeletedEvent,
    ModuleBackendConfigRepositorySettings>
{
    public ModuleBackendConfigController(ModuleBackendConfigService service) : base(service)
    {
    }

    [HttpGet("{moduleId}/{name}")]
    public async Task<ActionResult<ModuleBackendConfigReadDto>> Get(Guid organizationId, Guid moduleId, string name)
    {
        try
        {
            var moduleBackendConfigDto = await Service.Get(moduleId, name, organizationId);
            return Ok(moduleBackendConfigDto);
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