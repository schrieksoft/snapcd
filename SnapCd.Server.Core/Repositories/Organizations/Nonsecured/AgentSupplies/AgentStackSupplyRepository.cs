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
using SnapCd.Contracts.Dto.AgentStackSupplies;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.AgentSupplies;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.AgentSupplies;

public class AgentStackSupplyRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<AgentStackSupplyRepositorySettings> options)
{
    public AgentStackSupplyRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new AgentStackSupplyRepository(dbContext, principalProvider, bus, options);
    }
}

public class AgentStackSupplyRepository : GenericAgentChildRepository<AgentStackSupply, AgentStackSupplyReadDto, AgentStackSupplyCreatedEvent,
    AgentStackSupplyUpdatedEvent, AgentStackSupplyDeletedEvent, AgentStackSupplyRepositorySettings>
{
    public AgentStackSupplyRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<AgentStackSupplyRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override AgentStackSupplyReadDto MapToDto(AgentStackSupply entity)
    {
        return AgentStackSupplyMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(AgentStackSupply entity)
    {
        var currentCount = await DbContext.AgentStackSupplies
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.AgentStackSupplyQuota), currentCount);
    }

    public async Task<List<AgentStackSupply>> ListByAgent(Guid agentId, Guid organizationId)
    {
        return await DbContext.AgentStackSupplies
            .Where(a => a.OrganizationId == organizationId)
            .Where(a => a.AgentId == agentId)
            .ToListAsync();
    }

    public async Task<List<AgentStackSupply>> ListByStack(Guid stackId, Guid organizationId)
    {
        return await DbContext.AgentStackSupplies
            .Where(a => a.OrganizationId == organizationId)
            .Where(a => a.StackId == stackId)
            .ToListAsync();
    }
}
