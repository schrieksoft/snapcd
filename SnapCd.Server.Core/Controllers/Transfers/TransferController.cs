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
using SnapCd.Server.Core.Services.Crud.Transfers;

namespace SnapCd.Server.Core.Controllers.Transfers;

/// <summary>
/// The coordination surface for a transfer: consent, the gates, and closing it. The jobs that run
/// under a transfer are started from the Module's manual jobs, not here.
/// </summary>
[Route(ControllerEndpoints.Transfer)]
[ApiController]
[Authorize("BearerPolicy")]
[OrganizationScopedFeature]
public class TransferController : ControllerBase
{
    private readonly TransferServiceFactory _factory;

    public TransferController(TransferServiceFactory factory)
    {
        _factory = factory;
    }

    [EndpointSummary("Open a transfer from a source Module to one receiver")]
    [PermissionSource(Repository = typeof(ModuleSecuredRepository), Verb = PermissionVerb.Consent)]
    [HttpPost("{sourceModuleId}")]
    public Task<ActionResult> Create(
        Guid organizationId, Guid sourceModuleId, [FromBody] TransferCreateRequestDto request) =>
        Run(async service => (ActionResult)Ok(await service.Create(
            sourceModuleId, organizationId, request.Map, request.ReceiverModuleId, request.ProveRef)));

    [EndpointSummary("Answer a transfer's request for consent on behalf of a Module")]
    [PermissionSource(Repository = typeof(ModuleSecuredRepository), Verb = PermissionVerb.Consent)]
    [HttpPost("{transferId}/Module/{moduleId}/Consent")]
    public Task<ActionResult> Consent(
        Guid organizationId, Guid transferId, Guid moduleId, [FromBody] ConsentRequestDto request) =>
        Run(async service =>
        {
            await service.Decide(transferId, moduleId, organizationId, request.Granted, request.ProveRef, request.Reason);
            return (ActionResult)NoContent();
        });

    [EndpointSummary("Change the ref a Module wants proved, until it locks")]
    [PermissionSource(Repository = typeof(ModuleSecuredRepository), Verb = PermissionVerb.Consent)]
    [HttpPut("{transferId}/Module/{moduleId}/ProveRef")]
    public Task<ActionResult> SetProveRef(
        Guid organizationId, Guid transferId, Guid moduleId, [FromBody] ProveRefRequestDto request) =>
        Run(async service =>
        {
            await service.SetProveRef(transferId, moduleId, organizationId, request.Ref);
            return (ActionResult)NoContent();
        });

    [EndpointSummary("Withdraw a consent already given")]
    [PermissionSource(Repository = typeof(ModuleSecuredRepository), Verb = PermissionVerb.Consent)]
    [HttpPost("{transferId}/Module/{moduleId}/Consent/Revoke")]
    public Task<ActionResult> Revoke(
        Guid organizationId, Guid transferId, Guid moduleId, [FromBody] ReasonRequestDto? request) =>
        Run(async service =>
        {
            await service.Revoke(transferId, moduleId, organizationId, request?.Reason);
            return (ActionResult)NoContent();
        });

    [EndpointSummary("Hold a Module out of the automated lifecycle for this transfer, before merging")]
    [PermissionSource(Repository = typeof(ModuleSecuredRepository), Verb = PermissionVerb.Consent)]
    [HttpPost("{transferId}/Module/{moduleId}/Lock")]
    public Task<ActionResult> Lock(Guid organizationId, Guid transferId, Guid moduleId) =>
        Run(async service =>
        {
            await service.Lock(transferId, moduleId, organizationId);
            return (ActionResult)NoContent();
        });

    [EndpointSummary("Declare the proved code merged, naming the commit it landed as")]
    [PermissionSource(Repository = typeof(ModuleSecuredRepository), Verb = PermissionVerb.Consent)]
    [HttpPost("{transferId}/Module/{moduleId}/Merged")]
    public Task<ActionResult> DeclareMerged(
        Guid organizationId, Guid transferId, Guid moduleId, [FromBody] DeclareMergedRequestDto request) =>
        Run(async service =>
        {
            await service.DeclareMerged(transferId, moduleId, organizationId, request.MergedCommit);
            return (ActionResult)NoContent();
        });

    [EndpointSummary("Replace the transfer's map, re-asking the receiver for consent")]
    [PermissionSource(Repository = typeof(ModuleSecuredRepository), Verb = PermissionVerb.Consent)]
    [HttpPut("{transferId}/Map")]
    public Task<ActionResult> ReplaceMap(
        Guid organizationId, Guid transferId, [FromBody] ReplaceMapRequestDto request) =>
        Run(async service => (ActionResult)Ok(await service.ReplaceMap(
            transferId, organizationId, request.Map, TransferService.HashMap(request.Map))));

    [EndpointSummary("Close a transfer, pausing any Module it still holds")]
    [PermissionSource(Repository = typeof(ModuleSecuredRepository), Verb = PermissionVerb.Consent)]
    [HttpPost("{transferId}/Abandon")]
    public Task<ActionResult> Abandon(
        Guid organizationId, Guid transferId, [FromBody] ReasonRequestDto? request) =>
        Run(async service => (ActionResult)Ok(await service.Abandon(
            transferId, organizationId, request?.Reason ?? "abandoned")));

    [EndpointSummary("The transfer's status, recomputed from its participants and jobs")]
    [PermissionSource(Repository = typeof(ModuleSecuredRepository), Verb = PermissionVerb.Read)]
    [HttpGet("{transferId}/Status")]
    public Task<ActionResult> Status(Guid organizationId, Guid transferId) =>
        Run(async service => (ActionResult)Ok(await service.DeriveStatus(transferId, organizationId)));

    /// <summary>One translation of the service's refusals into status codes, for every action.</summary>
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
