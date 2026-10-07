// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.Groups;
using SnapCd.Server.Core.Controllers.Crud.Generic;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Filters;
using SnapCd.Server.Core.Misc.Attributes;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Controllers.Crud;

public static class GroupCustomEndpointNames
{
    public const string GetByName = "ByName";
}

[Route(ControllerEndpoints.Group)]
[OrganizationScopedIAM]
public class GroupController : GenericCrudController<
    Group,
    GroupCreateDto,
    GroupUpdateDto,
    GroupReadDto,
    GroupSecuredRepository,
    GroupRepository,
    GroupService,
    GroupCreatedEvent,
    GroupUpdatedEvent,
    GroupDeletedEvent,
    GroupRepositorySettings>
{
    public GroupController(GroupService service) : base(service)
    {
    }

    [HttpGet($"{GroupCustomEndpointNames.GetByName}/{{name}}")]
    public async Task<ActionResult<GroupReadDto>> GetByName(Guid organizationId, string name)
    {
        try
        {
            var groupDto = await Service.GetByName(name, organizationId);
            return Ok(groupDto);
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
    [PermissionSource(Repository = typeof(GroupSecuredRepository), Verb = PermissionVerb.ReadMetadata)]
    public async Task<ActionResult<List<GroupMetadataReadDto>>> ListMetadata(Guid organizationId)
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

    [HttpGet("Metadata/ByName/{name}")]
    [PermissionSource(Repository = typeof(GroupSecuredRepository), Verb = PermissionVerb.ReadMetadata)]
    public async Task<ActionResult<GroupMetadataReadDto>> GetMetadataByName(Guid organizationId, string name)
    {
        try
        {
            return Ok(await Service.GetMetadataByName(name, organizationId));
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

    [HttpGet("Metadata/{id}")]
    [PermissionSource(Repository = typeof(GroupSecuredRepository), Verb = PermissionVerb.ReadMetadata)]
    public async Task<ActionResult<GroupMetadataReadDto>> GetMetadata(Guid organizationId, Guid id)
    {
        try
        {
            return Ok(await Service.GetMetadata(id, organizationId));
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
