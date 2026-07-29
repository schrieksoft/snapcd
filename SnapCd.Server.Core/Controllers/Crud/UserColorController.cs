// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.UserColors;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Filters;
using SnapCd.Server.Core.Misc.Attributes;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Custom.Secured;
using SnapCd.Server.Core.Services.Notification;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Controllers.Crud;

[Route(ControllerEndpoints.UserColor)]
[ApiController]
[Authorize("BearerPolicy")]
[OrganizationScopedFeature]
[PermissionSource(Skip = true,
    Notes = "Per-user data: any authenticated member of the organization; rows are scoped to the calling principal.")]
public class UserColorController : ControllerBase
{
    private readonly UserColorSecuredRepositoryFactory _repositoryFactory;
    private readonly IPrincipalProvider _principalProvider;
    private readonly ColorsModifiedNotificationService _colorsModifiedNotificationService;

    public UserColorController(
        UserColorSecuredRepositoryFactory repositoryFactory,
        IPrincipalProvider principalProvider,
        ColorsModifiedNotificationService colorsModifiedNotificationService)
    {
        _repositoryFactory = repositoryFactory;
        _principalProvider = principalProvider;
        _colorsModifiedNotificationService = colorsModifiedNotificationService;
    }

    [HttpPut]
    public virtual async Task<ActionResult<UserColorReadDto?>> Set(Guid organizationId, UserColorCreateDto dto)
    {
        try
        {
            using var repository = _repositoryFactory.Create(_principalProvider);
            var color = await repository.Set(organizationId, dto.TargetType, dto.TargetId, dto.Color);
            await NotifyColorsModified(organizationId);
            return Ok(color == null ? null : ToReadDto(color));
        }
        catch (ArgumentException e)
        {
            return StatusCode(StatusCodes.Status400BadRequest, e.Message);
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

    [HttpGet]
    public virtual async Task<ActionResult<List<UserColorReadDto>>> List(Guid organizationId)
    {
        try
        {
            using var repository = _repositoryFactory.Create(_principalProvider);
            var colors = await repository.List(organizationId);
            return Ok(colors.Select(ToReadDto).ToList());
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

    [HttpDelete("{targetType}/{targetId}")]
    public virtual async Task<IActionResult> Delete(Guid organizationId, ColorTargetType targetType, Guid targetId)
    {
        try
        {
            using var repository = _repositoryFactory.Create(_principalProvider);
            await repository.Delete(organizationId, targetType, targetId);
            await NotifyColorsModified(organizationId);
            return NoContent();
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

    private async Task NotifyColorsModified(Guid organizationId)
    {
        await _colorsModifiedNotificationService.Notify(_principalProvider.GetSubject(organizationId));
    }

    private static UserColorReadDto ToReadDto(UserColor color)
    {
        return new UserColorReadDto
        {
            Id = color.Id,
            TargetType = color.TargetType,
            TargetId = color.TargetId,
            Color = color.Color
        };
    }
}
