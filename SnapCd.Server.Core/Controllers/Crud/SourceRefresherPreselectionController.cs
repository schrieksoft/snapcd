// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.SourceRefresherPreselections;
using SnapCd.Server.Core.Controllers.Crud.Generic;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Repositories.Organizations.Secured;
using SnapCd.Server.Core.Services.Crud;
using SnapCd.Server.Core.Settings.Repositories;
using SnapCd.Server.Core.Views;
using SnapCd.Server.Core.Views.Metadata;
using SnapCd.Server.Core.Misc.Attributes;

namespace SnapCd.Server.Core.Controllers.Crud;

public static class SourceRefresherPreselectionCustomEndpointNames
{
    public const string GetBySourceUrl = "BySourceUrl";

    /// <summary>
    /// The source URL as sent. Routing leaves an escaped slash escaped in a route value, so a client that escapes
    /// the URL into one path segment arrives with %2F in place of each slash.
    /// </summary>
    public static string SourceUrlFromRoute(string routeValue) =>
        routeValue.Replace("%2F", "/", StringComparison.OrdinalIgnoreCase);
}

[Route(ControllerEndpoints.SourceRefresherPreselection)]
public class SourceRefresherPreselectionController : GenericCrudController<
    SourceRefresherPreselection, SourceRefresherPreselectionMetadata,
    SourceRefresherPreselectionCreateDto,
    SourceRefresherPreselectionUpdateDto,
    SourceRefresherPreselectionReadDto,
    SourceRefresherPreselectionMetadataReadDto,
    SourceRefresherPreselectionSecuredRepository,
    SourceRefresherPreselectionRepository,
    SourceRefresherPreselectionService,
    SourceRefresherPreselectionCreatedEvent,
    SourceRefresherPreselectionUpdatedEvent,
    SourceRefresherPreselectionDeletedEvent,
    SourceRefresherPreselectionRepositorySettings>
{
    public SourceRefresherPreselectionController(SourceRefresherPreselectionService service) : base(service)
    {
    }

    // Catch-all: a source URL carries slashes, which a single segment cannot hold.
    [HttpGet($"{SourceRefresherPreselectionCustomEndpointNames.GetBySourceUrl}/{{**sourceUrl}}")]
    public async Task<ActionResult<SourceRefresherPreselectionReadDto>> GetByName(Guid organizationId, string sourceUrl)
    {
        try
        {
            var sourceRefresherPreselectionDto = await Service.GetBySourceUrl(
                SourceRefresherPreselectionCustomEndpointNames.SourceUrlFromRoute(sourceUrl), organizationId);
            return Ok(sourceRefresherPreselectionDto);
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

    [HttpGet($"Metadata/{SourceRefresherPreselectionCustomEndpointNames.GetBySourceUrl}/{{**sourceUrl}}")]
    [PermissionSource(Repository = typeof(SourceRefresherPreselectionSecuredRepository), Verb = PermissionVerb.ReadMetadata)]
    public async Task<ActionResult<SourceRefresherPreselectionMetadataReadDto>> GetMetadataBySourceUrl(Guid organizationId, string sourceUrl)
    {
        try
        {
            return Ok(await Service.GetMetadataBySourceUrl(
                SourceRefresherPreselectionCustomEndpointNames.SourceUrlFromRoute(sourceUrl), organizationId));
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