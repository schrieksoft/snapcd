using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Factories;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.Crud;

namespace SnapCd.Server.Core.Controllers.Crud;

[Route(ControllerEndpoints.ModuleInputFromSecret)]
[Authorize("BearerPolicy")]
public class ModuleInputFromSecretController : BaseController
{
    private readonly ModuleInputFromSecretServiceFactory _factory;
    private readonly ModuleInputFromSecretBaseService _baseService;

    public ModuleInputFromSecretController(ModuleInputFromSecretServiceFactory factory, ModuleInputFromSecretBaseService baseService)
    {
        _factory = factory;
        _baseService = baseService;
    }


    [HttpGet("{moduleId}/{name}")]
    public async Task<ActionResult<ModuleInputFromSecretReadDto>> Get(Guid organizationId, Guid moduleId, string name)
    {
        try
        {
            var result = await _baseService.Get(moduleId, name, organizationId);
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
    public async Task<ActionResult<ModuleInputFromSecretReadDto>> Get(Guid organizationId, Guid id)
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
    public async Task<ActionResult<ModuleInputFromSecretReadDto>> Create(Guid organizationId, [FromBody] ModuleInputFromSecretCreateDto dto)
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
    public async Task<ActionResult<ModuleInputFromSecretReadDto>> Update(Guid organizationId, Guid id, [FromBody] ModuleInputFromSecretUpdateDto dto)
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