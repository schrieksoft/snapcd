// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.Namespaces;
using SnapCd.Server.Core.Controllers.Crud.Generic;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Attributes;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Views;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Controllers.Crud;

[Route(ControllerEndpoints.Namespace)]
public class NamespaceController : GenericCrudController<
    Namespace, NamespaceMetadata,
    NamespaceCreateDto,
    NamespaceUpdateDto,
    NamespaceReadDto,
    NamespaceMetadataReadDto,
    NamespaceSecuredRepository,
    NamespaceRepository,
    NamespaceService,
    NamespaceCreatedEvent,
    NamespaceUpdatedEvent,
    NamespaceDeletedEvent,
    NamespaceRepositorySettings>
{
    public NamespaceController(NamespaceService service) : base(service)
    {
    }

    [HttpGet("{stackId:guid}/{name}")]
    public async Task<IActionResult> Get(Guid organizationId, Guid stackId, string name)
    {
        try
        {
            var namespaceDto = await Service.Get(stackId, name, organizationId);
            return Ok(namespaceDto);
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

    [HttpGet("Metadata/{stackId}/{name}")]
    [PermissionSource(Repository = typeof(NamespaceSecuredRepository), Verb = PermissionVerb.ReadMetadata)]
    public async Task<ActionResult<NamespaceMetadataReadDto>> GetMetadata(Guid organizationId, Guid stackId, string name)
    {
        try
        {
            return Ok(await Service.GetMetadata(stackId, name, organizationId));
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

    [HttpGet("Metadata")]
    [PermissionSource(Repository = typeof(NamespaceSecuredRepository), Verb = PermissionVerb.ReadMetadata)]
    public override async Task<ActionResult<List<NamespaceMetadataReadDto>>> ListMetadata(Guid organizationId)
    {
        try
        {
            return Ok(await Service.ListMetadata(organizationId));
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

    [HttpGet("Metadata/ByName/{stackName}/{name}")]
    [PermissionSource(Repository = typeof(NamespaceSecuredRepository), Verb = PermissionVerb.ReadMetadata)]
    public async Task<ActionResult<NamespaceMetadataReadDto>> GetMetadata(Guid organizationId, string stackName, string name)
    {
        try
        {
            return Ok(await Service.GetMetadata(stackName, name, organizationId));
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