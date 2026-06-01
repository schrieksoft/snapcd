// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Misc.Exceptions;

namespace SnapCd.Server.Core.Repositories.Custom.Nonsecured;

public class ModuleSagaRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory)
{
    public ModuleSagaRepository Create()
    {
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleSagaRepository(dbContext);
    }
}

public class ModuleSagaRepository : IDisposable
{
    private readonly SnapCdDbContext _dbContext;

    public ModuleSagaRepository(SnapCdDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public virtual async Task<ModuleSaga> Get(Guid correlationId, Guid organizationId)
    {
        var query = _dbContext.Set<ModuleSaga>().AsQueryable();

        var entity = await query
            .FirstOrDefaultAsync(i => i.CorrelationId == correlationId && i.OrganizationId == organizationId);

        if (entity == null)
            throw new EntityNotFoundException(
                $"{nameof(ModuleSaga)} with CorrelationId {correlationId} in Organization {organizationId} not found.");

        return entity;
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }
}