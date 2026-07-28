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
using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Filters;
using SnapCd.Server.Core.Misc.Attributes;
using SnapCd.Server.Core.Services;

namespace SnapCd.Server.Core.Controllers.Hooks;

[Route(ControllerEndpoints.SourceChangedNotification)]
[ApiController]
[Authorize("BearerPolicy")]
[OrganizationScopedFeature]
[PermissionSource(Repository = typeof(SourceChangedService), Verb = PermissionVerb.Create)]
public class SourceChangedNotificationController : ControllerBase
{
    private readonly SourceChangedService _sourceChangedService;

    public SourceChangedNotificationController(SourceChangedService sourceChangedService)
    {
        _sourceChangedService = sourceChangedService;
    }

    [HttpPost]
    public async Task<IActionResult> NotifyChange(Guid organizationId, SourceChangedDto dto)
    {
        try
        {
            await _sourceChangedService.NotifyChange(dto, organizationId);
            return Ok($"Source change has been notified");
        }
        catch (Exception e)
        {
            return StatusCode(StatusCodes.Status500InternalServerError, e.Message);
        }
    }
}