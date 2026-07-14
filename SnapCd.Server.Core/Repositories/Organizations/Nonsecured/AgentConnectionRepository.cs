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
using SnapCd.Server.Core.Dtos;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Events.Repository.Organization;
using SnapCd.Server.Core.Mappers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured.Generic;
using SnapCd.Server.Core.Services.PrincipalProvider;
using SnapCd.Server.Core.Settings.Repositories;

namespace SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

public class AgentConnectionRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<AgentConnectionRepositorySettings> options)
{
    public AgentConnectionRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new AgentConnectionRepository(dbContext, principalProvider, bus, options);
    }
}

public class AgentConnectionRepository : GenericOrganizationChildRepository<AgentConnection, AgentConnectionReadDto,
    AgentConnectionCreatedEvent, AgentConnectionUpdatedEvent, AgentConnectionDeletedEvent,
    AgentConnectionRepositorySettings>
{
    public AgentConnectionRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<AgentConnectionRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override AgentConnectionReadDto MapToDto(AgentConnection entity)
    {
        return AgentConnectionMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(AgentConnection entity)
    {
        var currentCount = await DbContext.AgentConnections
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.AgentConnectionQuota), currentCount);
    }

    /// <summary>
    /// Gets the active connection for a specific agent instance.
    /// Returns null if no active connection exists.
    /// </summary>
    public async Task<AgentConnection?> GetActiveConnection(Guid organizationId, Guid agentId, string instanceName)
    {
        return await DbContext.AgentConnections
            .Where(ac => ac.OrganizationId == organizationId &&
                         ac.AgentId == agentId &&
                         ac.InstanceName == instanceName)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Deletes the active connection for a specific agent instance.
    /// Used when an agent disconnects.
    /// </summary>
    public async Task DeleteConnection(Guid organizationId, Guid agentId, string instanceName)
    {
        var connection = await GetActiveConnection(organizationId, agentId, instanceName);
        if (connection != null)
        {
            DbContext.AgentConnections.Remove(connection);
            await DbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Deletes all connections owned by a specific server instance.
    /// Used by cleanup job when a server is detected as crashed/offline.
    /// </summary>
    public async Task DeleteConnectionsByServerId(Guid serverInstanceId)
    {
        var connections = await DbContext.AgentConnections
            .Where(ac => ac.ServerInstanceId == serverInstanceId)
            .ToListAsync();

        if (connections.Count > 0)
        {
            DbContext.AgentConnections.RemoveRange(connections);
            await DbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Gets all distinct server instance IDs that have active connections.
    /// </summary>
    public async Task<List<Guid>> GetDistinctServerInstanceIds()
    {
        return await DbContext.AgentConnections
            .Select(ac => ac.ServerInstanceId)
            .Distinct()
            .ToListAsync();
    }

    /// <summary>
    /// Gets an agent connection by SignalR connection ID.
    /// Used for authorization to map connectionId to agent identity.
    /// </summary>
    public async Task<AgentConnection?> GetBySignalRConnectionIdAsync(string signalRConnectionId, Guid organizationId)
    {
        return await DbContext.AgentConnections
            .Include(ac => ac.Agent)
            .FirstOrDefaultAsync(ac => ac.SignalRConnectionId == signalRConnectionId && ac.OrganizationId == organizationId);
    }

    /// <summary>
    /// Gets all active connections for a specific agent (all instances).
    /// </summary>
    public async Task<List<AgentConnection>> GetActiveConnectionsByAgentId(Guid agentId, Guid organizationId)
    {
        return await DbContext.AgentConnections
            .Include(ac => ac.Agent)
            .Where(ac => ac.OrganizationId == organizationId && ac.AgentId == agentId)
            .ToListAsync();
    }

    /// <summary>
    /// Gets the ids of agents that have at least one active connection in the organization.
    /// Used by the dashboard to compute connected counts in one query instead of one per agent.
    /// </summary>
    public async Task<HashSet<Guid>> GetConnectedAgentIds(Guid organizationId)
    {
        var agentIds = await DbContext.AgentConnections
            .Where(ac => ac.OrganizationId == organizationId)
            .Select(ac => ac.AgentId)
            .Distinct()
            .ToListAsync();
        return agentIds.ToHashSet();
    }

    /// <summary>
    /// Gets all connections owned by a specific server instance.
    /// </summary>
    public async Task<List<AgentConnection>> GetConnectionsByServerInstanceId(Guid serverInstanceId)
    {
        return await DbContext.AgentConnections
            .Where(ac => ac.ServerInstanceId == serverInstanceId)
            .ToListAsync();
    }

    /// <summary>
    /// Gets the active connection for a specific agent (any instance).
    /// </summary>
    public async Task<AgentConnection?> GetActiveConnectionByAgentId(Guid organizationId, Guid agentId)
    {
        return await DbContext.AgentConnections
            .Where(ac => ac.OrganizationId == organizationId && ac.AgentId == agentId)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// All active agent connections in an org. Used by the bus-to-hub bridge to fan out
    /// domain events to every connected agent in the affected organization.
    /// </summary>
    public async Task<List<AgentConnection>> GetActiveByOrganizationId(Guid organizationId)
    {
        return await DbContext.AgentConnections
            .Where(ac => ac.OrganizationId == organizationId)
            .ToListAsync();
    }
}
