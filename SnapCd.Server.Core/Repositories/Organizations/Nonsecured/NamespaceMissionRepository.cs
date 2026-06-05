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
using SnapCd.Contracts.Dto.Missions;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.Missions;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class NamespaceMissionRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<NamespaceMissionRepositorySettings> options)
{
    public NamespaceMissionRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new NamespaceMissionRepository(dbContext, principalProvider, bus, options);
    }
}

public class NamespaceMissionRepository : GenericNamespaceChildRepository<NamespaceMission, NamespaceMissionReadDto, NamespaceMissionCreatedEvent, NamespaceMissionUpdatedEvent, NamespaceMissionDeletedEvent, NamespaceMissionRepositorySettings>
{
    public NamespaceMissionRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<NamespaceMissionRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override NamespaceMissionReadDto MapToDto(NamespaceMission entity)
    {
        return NamespaceMissionMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(NamespaceMission entity)
    {
        var currentCount = await DbContext.NamespaceMissions
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.NamespaceMissionQuota), currentCount);
    }

    public async Task<List<NamespaceMission>> ListByAgent(Guid agentId, Guid organizationId)
    {
        return await DbContext.NamespaceMissions
            .Where(m => m.OrganizationId == organizationId && m.AgentId == agentId)
            .ToListAsync();
    }

    public async Task<List<NamespaceMission>> ListByNamespace(Guid namespaceId, Guid organizationId)
    {
        return await DbContext.NamespaceMissions
            .Where(m => m.OrganizationId == organizationId && m.NamespaceId == namespaceId)
            .ToListAsync();
    }
}
