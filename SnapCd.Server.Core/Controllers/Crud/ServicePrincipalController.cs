using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.ServicePrincipals;
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

public static class ServicePrincipalCustomEndpointNames
{
    public const string GetByClientId = "ByClientId";
    public const string GetWithVerifySecret = "WithVerifySecret";
}

[Route(ControllerEndpoints.ServicePrincipal)]
public class ServicePrincipalController : GenericCrudController<
    ServicePrincipal,
    ServicePrincipalCreateDto,
    ServicePrincipalUpdateDto,
    ServicePrincipalReadDto,
    ServicePrincipalSecuredRepository,
    ServicePrincipalRepository,
    ServicePrincipalService,
    ServicePrincipalCreatedEvent,
    ServicePrincipalUpdatedEvent,
    ServicePrincipalDeletedEvent,
    ServicePrincipalRepositorySettings>
{
    public ServicePrincipalController(ServicePrincipalService service) : base(service)
    {
    }

    # region Customized

    [HttpGet($"{ServicePrincipalCustomEndpointNames.GetByClientId}/{{name}}")]
    public async Task<ActionResult<ServicePrincipalReadDto>> GetByClientId(Guid organizationId, string name)
    {
        try
        {
            var servicePrincipalDto = await Service.GetByClientId(name, organizationId);
            return Ok(servicePrincipalDto);
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

    [HttpGet($"{ServicePrincipalCustomEndpointNames.GetWithVerifySecret}/{{id}}")]
    public async Task<ActionResult<ServicePrincipalReadDto>> GetWithSecretVerify(Guid organizationId, Guid id, [FromQuery] string? secret)
    {
        try
        {
            var servicePrincipalDto = await Service.GetWithSecretVerify(id, secret ?? string.Empty, organizationId);

            return Ok(servicePrincipalDto);
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

    # endregion
}