using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.NamespaceInputs;
using SnapCd.Server.Core.Factories;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.Crud;

namespace SnapCd.Server.Core.Controllers.Crud;

[Route(ControllerEndpoints.NamespaceInputFromDefinition)]
[Authorize("BearerPolicy")]
public class NamespaceInputFromDefinitionController : BaseController
{
    private readonly NamespaceInputFromDefinitionServiceFactory _factory;
    private readonly NamespaceInputFromDefinitionBaseService _baseService;

    public NamespaceInputFromDefinitionController(NamespaceInputFromDefinitionServiceFactory factory, NamespaceInputFromDefinitionBaseService baseService)
    {
        _factory = factory;
        _baseService = baseService;
    }

    [HttpGet("{namespaceId}/{name}")]
    public async Task<ActionResult<NamespaceInputFromDefinitionReadDto>> Get(Guid organizationId, Guid namespaceId, string name)
    {
        try
        {
            var result = await _baseService.Get(namespaceId, name, organizationId);
            return Ok(result);
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

    [HttpGet("{id}")]
    public async Task<ActionResult<NamespaceInputFromDefinitionReadDto>> Get(Guid organizationId, Guid id)
    {
        try
        {
            var result = await _baseService.Get(id, organizationId);
            return Ok(result);
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

    [HttpPost]
    public async Task<ActionResult<NamespaceInputFromDefinitionReadDto>> Create(Guid organizationId, [FromBody] NamespaceInputFromDefinitionCreateDto dto)
    {
        try
        {
            var service = _factory.GetService(dto.InputKind);
            var result = await service.Create(dto, organizationId);
            return Ok(result);
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

    [HttpPut("{id}")]
    public async Task<ActionResult<NamespaceInputFromDefinitionReadDto>> Update(Guid organizationId, Guid id, [FromBody] NamespaceInputFromDefinitionUpdateDto dto)
    {
        try
        {
            var service = _factory.GetService(dto.InputKind);
            var result = await service.Update(dto, id, organizationId);
            return Ok(result);
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

    [HttpDelete("{id}")]
    public async Task<ActionResult> Delete(Guid organizationId, Guid id)
    {
        try
        {
            await _baseService.Delete(id, organizationId);
            return Ok();
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