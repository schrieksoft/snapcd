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
using SnapCd.Contracts.Mcp;
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
[McpEntity(Singular = "Module", Plural = "Modules")]
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

    /// <summary>Source-repo coordinates for a Module: SourceType, SourceUrl, SourceRevision, SourceSubdirectory. The actual file contents are not returned by SnapCd — clone the repo directly using these coordinates.</summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="moduleId">Module ID</param>
    [HttpGet("{moduleId}/source")]
    [ExposeAsMcpResource(
        UriTemplate = "snapcd://orgs/{organizationId}/modules/{moduleId}/source",
        Name = "module_source")]
    public async Task<ActionResult<ModuleSourceDto>> GetSource(Guid organizationId, Guid moduleId)
    {
        try
        {
            return await Service.GetSource(moduleId, organizationId);
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

    /// <summary>Module state-status summary: latest actual state, desired state, current execution status, last job. Does NOT return the underlying state file (may contain secrets).</summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="moduleId">Module ID</param>
    [HttpGet("{moduleId}/state")]
    [ExposeAsMcpResource(
        UriTemplate = "snapcd://orgs/{organizationId}/modules/{moduleId}/state",
        Name = "module_state")]
    public async Task<ActionResult<ModuleStateDto>> GetState(Guid organizationId, Guid moduleId)
    {
        try
        {
            return await Service.GetState(moduleId, organizationId);
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