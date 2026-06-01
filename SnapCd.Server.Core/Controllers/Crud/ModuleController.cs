// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.Modules;
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

[Route(ControllerEndpoints.Module)]
public class ModuleController : GenericCrudController<
    Module,
    ModuleCreateDto,
    ModuleUpdateDto,
    ModuleReadDto,
    ModuleSecuredRepository,
    ModuleRepository,
    ModuleService,
    ModuleCreatedEvent,
    ModuleUpdatedEvent,
    ModuleDeletedEvent,
    ModuleRepositorySettings>
{
    public ModuleController(ModuleService service) : base(service)
    {
    }

    [HttpGet("{namespaceId}/{name}")]
    public async Task<ActionResult<ModuleReadDto>> Get(Guid organizationId, Guid namespaceId, string name)
    {
        try
        {
            var moduleDto = await Service.Get(namespaceId, name, organizationId);
            return Ok(moduleDto);
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

    [HttpGet("ByName/{stackName}/{namespaceName}/{moduleName}")]
    public async Task<ActionResult<ModuleReadDto>> GetByName(Guid organizationId, string stackName, string namespaceName, string moduleName)
    {
        try
        {
            var moduleDto = await Service.GetByName(stackName, namespaceName, moduleName, organizationId);
            return Ok(moduleDto);
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