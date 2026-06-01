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
using SnapCd.Contracts.Dto.Stacks;
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

public static class StackCustomEndpointNames
{
    public const string GetStackByName = "ByName";
}

[Route(ControllerEndpoints.Stack)]
[ApiController]
[Authorize("BearerPolicy")]
public class StackController : GenericCrudController<
    Stack,
    StackCreateDto,
    StackUpdateDto,
    StackReadDto,
    StackSecuredRepository,
    StackRepository,
    StackService,
    StackCreatedEvent,
    StackUpdatedEvent,
    StackDeletedEvent,
    StackRepositorySettings>
{
    public StackController(StackService service) : base(service)
    {
    }

    [HttpGet($"{StackCustomEndpointNames.GetStackByName}/{{name}}")]
    public async Task<ActionResult<StackReadDto>> GetByName(Guid organizationId, string name)
    {
        try
        {
            var stackDto = await Service.GetByName(name, organizationId);
            return Ok(stackDto);
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