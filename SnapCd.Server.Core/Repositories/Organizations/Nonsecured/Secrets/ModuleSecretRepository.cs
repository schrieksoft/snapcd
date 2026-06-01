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

public class ModuleSecretRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<ModuleSecretRepositorySettings> options)
{
    public ModuleSecretRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new ModuleSecretRepository(dbContext, principalProvider, bus, options);
    }
}

public class ModuleSecretRepository : GenericModuleChildDefinitionRepository<
    ModuleSecret,
    ModuleSecretDto,
    ModuleSecretCreatedEvent,
    ModuleSecretUpdatedEvent,
    ModuleSecretDeletedEvent,
    ModuleSecretRepositorySettings>
{
    public ModuleSecretRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<ModuleSecretRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override ModuleSecretDto MapToDto(ModuleSecret entity)
    {
        return ModuleSecretMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(ModuleSecret entity)
    {
        var currentCount = await DbContext.ModuleSecrets
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.ModuleSecretQuota), currentCount);
    }

    public async Task<ModuleSecret> GetByName(string name,
        Func<IQueryable<ModuleSecret>, IQueryable<ModuleSecret>>? include = null)
    {
        var query = DbContext.Set<ModuleSecret>().AsQueryable();

        if (include != null)
            query = include(query);

        var secret = await query
            .SingleOrDefaultAsync(i => i.Name == name);

        if (secret == null)
            throw new EntityNotFoundException($"{nameof(ModuleSecret)} with name {name} not found.");

        return secret;
    }

    public Task<List<ModuleSecret>> ListByIds(List<Guid> ids, Guid organizationId)
    {
        var secrets = DbContext.Set<ModuleSecret>()
            .Include(x => x.Organization)
            .Where(x => ids.Contains(x.Id) && x.OrganizationId == organizationId)
            .ToList();
        return Task.FromResult(secrets);
    }
}