using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.NamespaceBackendConfigs;
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

[Route(ControllerEndpoints.NamespaceBackendConfig)]
public class NamespaceBackendConfigController : GenericCrudController<
    NamespaceBackendConfig,
    NamespaceBackendConfigCreateDto,
    NamespaceBackendConfigUpdateDto,
    NamespaceBackendConfigReadDto,
    NamespaceBackendConfigSecuredRepository,
    NamespaceBackendConfigRepository,
    NamespaceBackendConfigService,
    NamespaceBackendConfigCreatedEvent,
    NamespaceBackendConfigUpdatedEvent,
    NamespaceBackendConfigDeletedEvent,
    NamespaceBackendConfigRepositorySettings>
{
    public NamespaceBackendConfigController(NamespaceBackendConfigService service) : base(service)
    {
    }

    [HttpGet("{namespaceId}/{name}")]
    public async Task<ActionResult<NamespaceBackendConfigReadDto>> Get(Guid organizationId, Guid namespaceId, string name)
    {
        try
        {
            var namespaceBackendConfigDto = await Service.Get(namespaceId, name, organizationId);
            return Ok(namespaceBackendConfigDto);
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