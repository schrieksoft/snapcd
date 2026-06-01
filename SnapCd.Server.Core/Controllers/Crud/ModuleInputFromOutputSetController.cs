// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.ModuleInputs;
using SnapCd.Server.Core.Factories;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.Crud;

namespace SnapCd.Server.Core.Controllers.Crud;

[Route(ControllerEndpoints.ModuleInputFromOutputSet)]
[Authorize("BearerPolicy")]
public class ModuleInputFromOutputSetController : BaseController
{
    private readonly ModuleInputFromOutputSetServiceFactory _factory;
    private readonly ModuleInputFromOutputSetBaseService _baseService;

    public ModuleInputFromOutputSetController(ModuleInputFromOutputSetServiceFactory factory, ModuleInputFromOutputSetBaseService baseService)
    {
        _factory = factory;
        _baseService = baseService;
    }

    [HttpGet("{moduleId}/{name}")]
    public async Task<ActionResult<ModuleInputFromOutputSetReadDto>> Get(Guid organizationId, Guid moduleId, string name)
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
    public async Task<ActionResult<ModuleInputFromOutputSetReadDto>> Get(Guid organizationId, Guid id)
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
    public async Task<ActionResult<ModuleInputFromOutputSetReadDto>> Create(Guid organizationId, [FromBody] ModuleInputFromOutputSetCreateDto dto)
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
    public async Task<ActionResult<ModuleInputFromOutputSetReadDto>> Update(Guid organizationId, Guid id, [FromBody] ModuleInputFromOutputSetUpdateDto dto)
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