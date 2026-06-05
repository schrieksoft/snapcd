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
using SnapCd.Contracts.Dto.AgentNamespaceAssignments;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition.AgentAssignments;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured.AgentAssignments;

public class AgentNamespaceAssignmentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<AgentNamespaceAssignmentRepositorySettings> options)
{
    public AgentNamespaceAssignmentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new AgentNamespaceAssignmentRepository(dbContext, principalProvider, bus, options);
    }
}

public class AgentNamespaceAssignmentRepository : GenericOrganizationChildRepository<AgentNamespaceAssignment, AgentNamespaceAssignmentReadDto, AgentNamespaceAssignmentCreatedEvent,
    AgentNamespaceAssignmentUpdatedEvent, AgentNamespaceAssignmentDeletedEvent, AgentNamespaceAssignmentRepositorySettings>
{
    public AgentNamespaceAssignmentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<AgentNamespaceAssignmentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override AgentNamespaceAssignmentReadDto MapToDto(AgentNamespaceAssignment entity)
    {
        return AgentNamespaceAssignmentMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(AgentNamespaceAssignment entity)
    {
        var currentCount = await DbContext.AgentNamespaceAssignments
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.AgentNamespaceAssignmentQuota), currentCount);
    }

    public async Task<List<AgentNamespaceAssignment>> ListByAgent(Guid agentId, Guid organizationId)
    {
        return await DbContext.AgentNamespaceAssignments
            .Where(a => a.OrganizationId == organizationId)
            .Where(a => a.AgentId == agentId)
            .ToListAsync();
    }

    public async Task<List<AgentNamespaceAssignment>> ListByNamespace(Guid namespaceId, Guid organizationId)
    {
        return await DbContext.AgentNamespaceAssignments
            .Where(a => a.OrganizationId == organizationId)
            .Where(a => a.NamespaceId == namespaceId)
            .ToListAsync();
    }
}
