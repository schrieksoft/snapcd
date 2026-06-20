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
using SnapCd.Contracts.Dto.RunnerStackSupplies;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RunnerSupplies;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RunnerSupplies;

public class RunnerStackSupplyRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<RunnerStackSupplyRepositorySettings> options)
{
    public RunnerStackSupplyRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new RunnerStackSupplyRepository(dbContext, principalProvider, bus, options);
    }
}

public class RunnerStackSupplyRepository : GenericOrganizationChildRepository<RunnerStackSupply, RunnerStackSupplyReadDto, RunnerStackSupplyCreatedEvent,
    RunnerStackSupplyUpdatedEvent, RunnerStackSupplyDeletedEvent, RunnerStackSupplyRepositorySettings>
{
    public RunnerStackSupplyRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<RunnerStackSupplyRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override RunnerStackSupplyReadDto MapToDto(RunnerStackSupply entity)
    {
        return RunnerStackSupplyMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(RunnerStackSupply entity)
    {
        var currentCount = await DbContext.RunnerStackSupplies
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.RunnerStackSupplyQuota), currentCount);
    }

    public async Task<List<RunnerStackSupply>> ListByRunner(Guid runnerId, Guid organizationId)
    {
        return await DbContext.RunnerStackSupplies
            .Where(a => a.OrganizationId == organizationId)
            .Where(a => a.RunnerId == runnerId)
            .ToListAsync();
    }

    public async Task<List<RunnerStackSupply>> ListByStack(Guid stackId, Guid organizationId)
    {
        return await DbContext.RunnerStackSupplies
            .Where(a => a.OrganizationId == organizationId)
            .Where(a => a.StackId == stackId)
            .ToListAsync();
    }
}