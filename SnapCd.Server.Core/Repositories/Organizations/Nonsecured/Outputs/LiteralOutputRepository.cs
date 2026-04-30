using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Outputs;

public class LiteralOutputRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<OutputRepositorySettings> options)
{
    public LiteralOutputRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new LiteralOutputRepository(dbContext, principalProvider, bus, options);
    }
}

public class LiteralOutputRepository : OutputRepository
{
    public LiteralOutputRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<OutputRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    public Task<List<LiteralOutput>> ListByIds(List<Guid> ids, Guid organizationId)
    {
        var outputs = DbContext.Set<LiteralOutput>()
            .Include(x => x.Organization)
            .Where(x => ids.Contains(x.Id) && x.OrganizationId == organizationId)
            .ToList();
        return Task.FromResult(outputs);
    }
}