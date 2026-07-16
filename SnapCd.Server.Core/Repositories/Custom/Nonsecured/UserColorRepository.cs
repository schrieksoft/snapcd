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

public class UserColorRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory)
{
    public UserColorRepository Create()
    {
        var dbContext = dbFactory.CreateDbContext();
        return new UserColorRepository(dbContext);
    }
}

public class UserColorRepository : IDisposable
{
    private readonly SnapCdDbContext _dbContext;

    public UserColorRepository(SnapCdDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<List<UserColor>> ListByUser(Guid userId, Guid organizationId)
    {
        return await _dbContext.UserColors
            .Where(c => c.UserId == userId && c.OrganizationId == organizationId)
            .ToListAsync();
    }

    public async Task<UserColor?> GetByTarget(
        Guid userId,
        Guid organizationId,
        ColorTargetType targetType,
        Guid targetId)
    {
        return await _dbContext.UserColors
            .Where(c => c.UserId == userId
                        && c.OrganizationId == organizationId
                        && c.TargetType == targetType
                        && c.TargetId == targetId)
            .SingleOrDefaultAsync();
    }

    /// <summary>
    /// Assigns a colour to a target, replacing any colour already set on it. Unlike a
    /// favourite (which is a toggle), a colour is one row per target that gets changed —
    /// hence upsert rather than insert.
    /// </summary>
    public async Task<UserColor> Upsert(UserColor color)
    {
        var existing = await GetByTarget(color.UserId, color.OrganizationId, color.TargetType, color.TargetId);

        if (existing == null)
        {
            _dbContext.UserColors.Add(color);
            await _dbContext.SaveChangesAsync();
            return color;
        }

        existing.Color = color.Color;
        existing.ModifiedBy = color.ModifiedBy;
        existing.ModifiedByPrincipalDiscriminator = color.ModifiedByPrincipalDiscriminator;
        existing.ModifiedDateTime = color.ModifiedDateTime;
        await _dbContext.SaveChangesAsync();
        return existing;
    }

    /// <summary>Clears the colour on a target. No-op if none was set.</summary>
    public async Task DeleteByTarget(
        Guid userId,
        Guid organizationId,
        ColorTargetType targetType,
        Guid targetId)
    {
        await _dbContext.UserColors
            .Where(c => c.UserId == userId
                        && c.OrganizationId == organizationId
                        && c.TargetType == targetType
                        && c.TargetId == targetId)
            .ExecuteDeleteAsync();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}
