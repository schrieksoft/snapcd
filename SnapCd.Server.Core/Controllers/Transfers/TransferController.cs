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
using SnapCd.Contracts.Dto.Transfers;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Filters;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Attributes;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud.Jobs;
using SnapCd.Server.Core.Services.Crud.Transfers;

namespace SnapCd.Server.Core.Controllers.Transfers;

/// <summary>
/// A transfer's own surface: opening it, and the receiver's consent. The jobs that move the state
/// are ordinary manual jobs, started from the Module.
/// </summary>
[Route(ControllerEndpoints.Transfer)]
[ApiController]
[Authorize("BearerPolicy")]
[OrganizationScopedFeature]
public class TransferController : ControllerBase
{
    private readonly TransferServiceFactory _factory;
    private readonly TransferOpenerFactory _openerFactory;

    public TransferController(TransferServiceFactory factory, TransferOpenerFactory openerFactory)
    {
        _factory = factory;
        _openerFactory = openerFactory;
    }

    [EndpointSummary("Open a transfer from a source Module to one receiver, reading the map from a ref")]
    [PermissionSource(Repository = typeof(ModuleSecuredRepository), Verb = PermissionVerb.Consent)]
    [HttpPost("{sourceModuleId}")]
    public async Task<ActionResult> Create(
        Guid organizationId, Guid sourceModuleId, [FromBody] TransferCreateRequestDto request)
    {
        try
        {
            var opener = _openerFactory.Create();
            var transfer = await opener.Open(
                sourceModuleId, request.ReceiverModuleId, organizationId, request.ProveRef);

            return Ok(transfer);
        }
        catch (EntityNotFoundException e)
        {
            return StatusCode(CustomStatusCodes.Status441EntityNotFound, e.Message);
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
        catch (ManualJobNotAllowedException e)
        {
            return Conflict(e.Message);
        }
    }

    [EndpointSummary("Answer a transfer's request for consent on behalf of the receiving Module")]
    [PermissionSource(Repository = typeof(ModuleSecuredRepository), Verb = PermissionVerb.Consent)]
    [HttpPost("{transferId}/Module/{moduleId}/Consent")]
    public Task<ActionResult> Consent(
        Guid organizationId, Guid transferId, Guid moduleId, [FromBody] ConsentRequestDto request) =>
        Run(async service =>
        {
            await service.Decide(
                transferId, moduleId, organizationId, request.Granted, request.ProveRef, request.Reason);

            return (ActionResult)Ok();
        });

    [EndpointSummary("Where the transfer stands, read from its jobs")]
    [PermissionSource(Repository = typeof(ModuleSecuredRepository), Verb = PermissionVerb.Read)]
    [HttpGet("{transferId}/Status")]
    public Task<ActionResult> Status(Guid organizationId, Guid transferId) =>
        Run(async service => (ActionResult)Ok(await service.DeriveStatus(transferId, organizationId)));

    private async Task<ActionResult> Run(Func<TransferService, Task<ActionResult>> action)
    {
        using var service = _factory.Create();
        try
        {
            return await action(service);
        }
        catch (EntityNotFoundException e)
        {
            return StatusCode(CustomStatusCodes.Status441EntityNotFound, e.Message);
        }
        catch (PrincipalNotAuthorizedException e)
        {
            return StatusCode(StatusCodes.Status403Forbidden, e.Message);
        }
        catch (ManualJobNotAllowedException e)
        {
            return Conflict(e.Message);
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
    }
}
