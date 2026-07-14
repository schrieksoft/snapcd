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

namespace SnapCd.Server.Core.Repositories.Custom.Nonsecured;

public class UserFavoriteRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory)
{
    public UserFavoriteRepository Create()
    {
        var dbContext = dbFactory.CreateDbContext();
        return new UserFavoriteRepository(dbContext);
    }
}

public class UserFavoriteRepository : IDisposable
{
    private readonly SnapCdDbContext _dbContext;

    public UserFavoriteRepository(SnapCdDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<UserFavorite>> ListByUser(Guid userId, Guid organizationId)
    {
        return await _dbContext.UserFavorites
            .Where(f => f.UserId == userId && f.OrganizationId == organizationId)
            .OrderBy(f => f.CreatedDateTime)
            .ToListAsync();
    }

    public async Task<UserFavorite?> GetByTarget(
        Guid userId,
        Guid organizationId,
        FavoriteTargetType targetType,
        Guid targetId)
    {
        return await _dbContext.UserFavorites
            .Where(f => f.UserId == userId
                        && f.OrganizationId == organizationId
                        && f.TargetType == targetType
                        && f.TargetId == targetId)
            .SingleOrDefaultAsync();
    }

    public async Task Create(UserFavorite favorite)
    {
        _dbContext.UserFavorites.Add(favorite);
        await _dbContext.SaveChangesAsync();
    }

    public async Task Delete(Guid id, Guid userId, Guid organizationId)
    {
        await _dbContext.UserFavorites
            .Where(f => f.Id == id && f.UserId == userId && f.OrganizationId == organizationId)
            .ExecuteDeleteAsync();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
