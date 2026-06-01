// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Outputs;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Outputs;

public class SecretOutputRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<OutputRepositorySettings> options)
{
    public SecretOutputRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new SecretOutputRepository(dbContext, principalProvider, bus, options);
    }
}

public class SecretOutputRepository : OutputRepository
{
    public SecretOutputRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<OutputRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    public async Task<SecretOutput> GetByName(string name,
        Func<IQueryable<SecretOutput>, IQueryable<SecretOutput>>? include = null)
    {
        var query = DbContext.Set<SecretOutput>().AsQueryable();

        // modify the query with "include" delegates
        if (include != null)
            query = include(query);

        var secret = await query
            .SingleOrDefaultAsync(i => i.Name == name);

        if (secret == null)
            throw new EntityNotFoundException($"{nameof(SecretOutput)} with name {name} not found.");

        return secret;
    }

    public new Task<List<SecretOutput>> ListByIds(List<Guid> ids, Guid organizationId)
    {
        var secrets = DbContext.Set<SecretOutput>()
            .Include(x => x.Organization)
            .Where(x => ids.Contains(x.Id) && x.OrganizationId == organizationId)
            .ToList();
        return Task.FromResult(secrets);
    }
}