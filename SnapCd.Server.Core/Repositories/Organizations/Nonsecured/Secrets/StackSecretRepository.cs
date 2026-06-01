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
using SnapCd.Contracts.Dto.Secrets.Scoped;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Secrets.Scoped;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Secrets;

public class StackSecretRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<StackSecretRepositorySettings> options)
{
    public StackSecretRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new StackSecretRepository(dbContext, principalProvider, bus, options);
    }
}

public class StackSecretRepository : GenericStackChildDefinitionRepository<
    StackSecret,
    StackSecretDto,
    StackSecretCreatedEvent,
    StackSecretUpdatedEvent,
    StackSecretDeletedEvent,
    StackSecretRepositorySettings>
{
    public StackSecretRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<StackSecretRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override StackSecretDto MapToDto(StackSecret entity)
    {
        return StackSecretMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(StackSecret entity)
    {
        var currentCount = await DbContext.StackSecrets
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.StackSecretQuota), currentCount);
    }

    public async Task<StackSecret> GetByName(string name,
        Func<IQueryable<StackSecret>, IQueryable<StackSecret>>? include = null)
    {
        var query = DbContext.Set<StackSecret>().AsQueryable();

        if (include != null)
            query = include(query);

        var secret = await query
            .SingleOrDefaultAsync(i => i.Name == name);

        if (secret == null)
            throw new EntityNotFoundException($"{nameof(StackSecret)} with name {name} not found.");

        return secret;
    }

    public Task<List<StackSecret>> ListByIds(List<Guid> ids, Guid organizationId)
    {
        var secrets = DbContext.Set<StackSecret>()
            .Include(x => x.Organization)
            .Where(x => ids.Contains(x.Id) && x.OrganizationId == organizationId)
            .ToList();
        return Task.FromResult(secrets);
    }
}