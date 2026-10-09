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
using System.Linq.Expressions;
using SnapCd.Server.Core.Views;
using SnapCd.Server.Core.Views.Metadata;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class OrganizationMissionRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<OrganizationMissionRepositorySettings> options)
{
    public OrganizationMissionRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new OrganizationMissionRepository(dbContext, principalProvider, bus, options);
    }
}

public class OrganizationMissionRepository : GenericOrganizationChildRepository<OrganizationMission, OrganizationMissionReadDto, OrganizationMissionMetadata, OrganizationMissionCreatedEvent, OrganizationMissionUpdatedEvent, OrganizationMissionDeletedEvent, OrganizationMissionRepositorySettings>
{

    protected override Expression<Func<OrganizationMission, OrganizationMissionMetadata>> MetadataProjection =>
        e => new OrganizationMissionMetadata
        {
            Id = e.Id,
            OrganizationId = e.OrganizationId,
            AgentId = e.AgentId
        };
    public OrganizationMissionRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<OrganizationMissionRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override OrganizationMissionReadDto MapToDto(OrganizationMission entity)
    {
        return OrganizationMissionMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(OrganizationMission entity)
    {
        var currentCount = await DbContext.OrganizationMissions
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.OrganizationMissionQuota), currentCount);
    }

    public async Task<List<OrganizationMission>> ListByAgent(Guid agentId, Guid organizationId)
    {
        return await DbContext.OrganizationMissions
            .Where(m => m.OrganizationId == organizationId && m.AgentId == agentId)
            .ToListAsync();
    }
}
