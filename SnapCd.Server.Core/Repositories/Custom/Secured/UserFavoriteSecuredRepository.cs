// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Custom.Nonsecured;
using SnapCd.Server.Core.Services.PrincipalProvider;

namespace SnapCd.Server.Core.Repositories.Custom.Secured;

public class UserFavoriteSecuredRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory)
{
    public UserFavoriteSecuredRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var repositoryFactory = new UserFavoriteRepositoryFactory(dbFactory);
        return new UserFavoriteSecuredRepository(repositoryFactory.Create(), principalProvider);
    }
}

/// <summary>
/// Favorites are strictly personal: every operation is scoped to the current principal's user id.
/// Only users have favorites — service principals get empty reads and are not allowed to write.
/// </summary>
public class UserFavoriteSecuredRepository : IDisposable
{
    private readonly UserFavoriteRepository _repository;
    private readonly IPrincipalProvider _principalProvider;

    public UserFavoriteSecuredRepository(UserFavoriteRepository repository, IPrincipalProvider principalProvider)
    {
        _repository = repository;
        _principalProvider = principalProvider;
    }

    private bool IsUser => _principalProvider.GetPrincipalDiscriminator() == PrincipalDiscriminator.User;

    public async Task<List<UserFavorite>> List(Guid organizationId)
    {
        if (!IsUser) return new List<UserFavorite>();
        return await _repository.ListByUser(_principalProvider.GetSubject(organizationId), organizationId);
    }

    public async Task<UserFavorite?> GetByTarget(Guid organizationId, FavoriteTargetType targetType, Guid targetId)
    {
        if (!IsUser) return null;
        return await _repository.GetByTarget(_principalProvider.GetSubject(organizationId), organizationId, targetType, targetId);
    }

    public async Task<UserFavorite> Create(Guid organizationId, FavoriteTargetType targetType, Guid targetId)
    {
        if (!IsUser)
            throw new PrincipalNotAuthorizedException("Only users can create favorites.");

        var userId = _principalProvider.GetSubject(organizationId);
        var favorite = new UserFavorite
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            UserId = userId,
            TargetType = targetType,
            TargetId = targetId,
            CreatedBy = userId,
            CreatedByPrincipalDiscriminator = AuditPrincipalDiscriminator.User,
            CreatedDateTime = DateTime.UtcNow,
            ModifiedBy = userId,
            ModifiedByPrincipalDiscriminator = AuditPrincipalDiscriminator.User,
            ModifiedDateTime = DateTime.UtcNow
        };
        await _repository.Create(favorite);
        return favorite;
    }

    public async Task Delete(Guid id, Guid organizationId)
    {
        if (!IsUser)
            throw new PrincipalNotAuthorizedException("Only users can delete favorites.");

        // Nonsecured delete is filtered by user id, so users can only ever delete their own favorites
        await _repository.Delete(id, _principalProvider.GetSubject(organizationId), organizationId);
    }

    public void Dispose()
    {
        _repository.Dispose();
    }
}
