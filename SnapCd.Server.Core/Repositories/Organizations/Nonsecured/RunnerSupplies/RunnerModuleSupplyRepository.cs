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
using SnapCd.Contracts.Dto.RunnerModuleSupplies;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.RunnerSupplies;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.RunnerSupplies;

public class RunnerModuleSupplyRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<RunnerModuleSupplyRepositorySettings> options)
{
    public RunnerModuleSupplyRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new RunnerModuleSupplyRepository(dbContext, principalProvider, bus, options);
    }
}

public class RunnerModuleSupplyRepository : GenericOrganizationChildRepository<RunnerModuleSupply, RunnerModuleSupplyReadDto, RunnerModuleSupplyCreatedEvent,
    RunnerModuleSupplyUpdatedEvent, RunnerModuleSupplyDeletedEvent, RunnerModuleSupplyRepositorySettings>
{
    public RunnerModuleSupplyRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<RunnerModuleSupplyRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override RunnerModuleSupplyReadDto MapToDto(RunnerModuleSupply entity)
    {
        return RunnerModuleSupplyMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(RunnerModuleSupply entity)
    {
        var currentCount = await DbContext.RunnerModuleSupplies
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.RunnerModuleSupplyQuota), currentCount);
    }

    public async Task<List<RunnerModuleSupply>> ListByRunner(Guid runnerId, Guid organizationId)
    {
        return await DbContext.RunnerModuleSupplies
            .Where(a => a.OrganizationId == organizationId)
            .Where(a => a.RunnerId == runnerId)
            .ToListAsync();
    }

    public async Task<List<RunnerModuleSupply>> ListByModule(Guid moduleId, Guid organizationId)
    {
        return await DbContext.RunnerModuleSupplies
            .Where(a => a.OrganizationId == organizationId)
            .Where(a => a.ModuleId == moduleId)
            .ToListAsync();
    }
}