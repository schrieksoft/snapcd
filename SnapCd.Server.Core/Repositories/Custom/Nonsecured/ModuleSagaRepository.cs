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