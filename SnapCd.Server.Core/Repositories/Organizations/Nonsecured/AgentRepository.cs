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
using SnapCd.Contracts;
using SnapCd.Contracts.Dto.Agents;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Entities.Definition.RoleAssignments.Org;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Misc.Exceptions;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class AgentRepositoryFactory(IDbContextFactory<SnapCdDbContext> dbFactory, IPublishEndpoint bus, IOptions<AgentRepositorySettings> options)
{
    public AgentRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new AgentRepository(dbContext, principalProvider, bus, options);
    }
}

public class AgentRepository : GenericOrganizationChildRepository<Agent, AgentReadDto, AgentCreatedEvent, AgentUpdatedEvent, AgentDeletedEvent, AgentRepositorySettings>
{
    public AgentRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<AgentRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override async Task SetServicePrincipalOwner(Guid id, Guid organizationId, Guid servicePrincipalId)
    {
        DbContext.ServicePrincipalAgentRoleAssignments.Add(new ServicePrincipalAgentRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AgentId = id,
            ServicePrincipalId = servicePrincipalId,
            RoleName = AgentRole.Owner
        });
    }

    protected override async Task SetUserOwner(Guid id, Guid organizationId, Guid userId)
    {
        DbContext.UserAgentRoleAssignments.Add(new UserAgentRoleAssignment
        {
            Id = Guid.NewGuid(),
            OrganizationId = organizationId,
            AgentId = id,
            UserId = userId,
            RoleName = AgentRole.Owner
        });
    }

    protected override AgentReadDto MapToDto(Agent entity)
    {
        return AgentMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(Agent entity)
    {
        var currentCount = await DbContext.Agents
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.AgentQuota), currentCount);
    }

    public async Task<Agent> GetByName(string name, Guid organizationId)
    {
        var agent = await DbContext.Agents
            .Where(a => a.OrganizationId == organizationId)
            .SingleOrDefaultAsync(i => i.Name == name);

        if (agent == null) throw new EntityNotFoundException($"Agent with name {name} not found.");

        return agent;
    }

    public async Task<Agent?> GetByServicePrincipalId(Guid servicePrincipalId, Guid organizationId)
    {
        return await DbContext.Agents
            .Where(a => a.OrganizationId == organizationId && a.ServicePrincipalId == servicePrincipalId)
            .FirstOrDefaultAsync();
    }
}
