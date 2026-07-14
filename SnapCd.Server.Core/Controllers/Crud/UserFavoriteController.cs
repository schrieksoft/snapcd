// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using EntityFramework.Exceptions.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.Dto.UserFavorites;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Filters;
using SnapCd.Server.Core.Misc.Constants;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Custom.Secured;
using SnapCd.Server.Core.Services.Notification;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Controllers.Crud;

/// <summary>
/// Favorites (starred stacks/namespaces/modules) of the calling user. Favorites are strictly
/// personal: all operations are scoped to the authenticated principal, and only user principals
/// may read or write (service principals get 403 on writes and an empty list on reads).
/// </summary>
[Route(ControllerEndpoints.UserFavorite)]
[ApiController]
[Authorize("BearerPolicy")]
[OrganizationScopedFeature]
public class UserFavoriteController : ControllerBase
{
    private readonly UserFavoriteSecuredRepositoryFactory _repositoryFactory;
    private readonly IPrincipalProvider _principalProvider;
    private readonly FavoritesModifiedNotificationService _favoritesModifiedNotificationService;

    public UserFavoriteController(
        UserFavoriteSecuredRepositoryFactory repositoryFactory,
        IPrincipalProvider principalProvider,
        FavoritesModifiedNotificationService favoritesModifiedNotificationService)
    {
        _repositoryFactory = repositoryFactory;
        _principalProvider = principalProvider;
        _favoritesModifiedNotificationService = favoritesModifiedNotificationService;
    }

    [HttpPost]
    public virtual async Task<ActionResult<UserFavoriteReadDto>> Create(Guid organizationId, UserFavoriteCreateDto dto)
    {
        try
        {
            using var repository = _repositoryFactory.Create(_principalProvider);
            var favorite = await repository.Create(organizationId, dto.TargetType, dto.TargetId);
            await NotifyFavoritesModified(organizationId);
            return Ok(ToReadDto(favorite));
        }
        catch (UniqueConstraintException e)
        {
            return StatusCode(CustomStatusCodes.Status442EntityAlreadyExists, e.Message);
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
    public virtual async Task<ActionResult<List<UserFavoriteReadDto>>> List(Guid organizationId)
    {
        try
        {
            using var repository = _repositoryFactory.Create(_principalProvider);
            var favorites = await repository.List(organizationId);
            return Ok(favorites.Select(ToReadDto).ToList());
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

    [HttpDelete("{id}")]
    public virtual async Task<IActionResult> Delete(Guid organizationId, Guid id)
    {
        try
        {
            using var repository = _repositoryFactory.Create(_principalProvider);
            await repository.Delete(id, organizationId);
            await NotifyFavoritesModified(organizationId);
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

    private async Task NotifyFavoritesModified(Guid organizationId)
    {
        await _favoritesModifiedNotificationService.Notify(_principalProvider.GetSubject(organizationId));
    }

    private static UserFavoriteReadDto ToReadDto(UserFavorite favorite)
    {
        return new UserFavoriteReadDto
        {
            Id = favorite.Id,
            TargetType = favorite.TargetType,
            TargetId = favorite.TargetId
        };
    }
}
