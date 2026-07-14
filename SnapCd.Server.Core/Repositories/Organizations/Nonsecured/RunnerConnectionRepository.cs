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

public class RunnerConnectionRepositoryFactory(
    IDbContextFactory<SnapCdDbContext> dbFactory,
    IPublishEndpoint bus,
    IOptions<RunnerConnectionRepositorySettings> options)
{
    public RunnerConnectionRepository Create(IPrincipalProvider? principalProvider = null)
    {
        if (principalProvider == null)
            principalProvider = new HttpContextPrincipalProvider(new HttpContextAccessor());
        var dbContext = dbFactory.CreateDbContext();
        return new RunnerConnectionRepository(dbContext, principalProvider, bus, options);
    }
}

public class RunnerConnectionRepository : GenericOrganizationChildRepository<RunnerConnection, RunnerConnectionReadDto,
    RunnerConnectionCreatedEvent, RunnerConnectionUpdatedEvent, RunnerConnectionDeletedEvent,
    RunnerConnectionRepositorySettings>
{
    public RunnerConnectionRepository(
        SnapCdDbContext dbContext,
        IPrincipalProvider principalProvider,
        IPublishEndpoint bus,
        IOptions<RunnerConnectionRepositorySettings> options)
        : base(dbContext, principalProvider, bus, options)
    {
    }

    protected override RunnerConnectionReadDto MapToDto(RunnerConnection entity)
    {
        return RunnerConnectionMapper.ToDto(entity);
    }

    protected override async Task<QuotaCheckResult> CheckQuotaAsync(RunnerConnection entity)
    {
        var currentCount = await DbContext.RunnerConnections
            .CountAsync(e => e.OrganizationId == entity.OrganizationId);

        return await CheckQuotaWithServiceAsync(entity.OrganizationId, nameof(Settings.QuotaLimits.RunnerConnectionQuota), currentCount);
    }

    /// <summary>
    /// Gets the active connection for a specific runner instance.
    /// Returns null if no active connection exists.
    /// </summary>
    public async Task<RunnerConnection?> GetActiveConnection(Guid organizationId, Guid runnerId, string instanceName)
    {
        return await DbContext.RunnerConnections
            .Where(rc => rc.OrganizationId == organizationId &&
                         rc.RunnerId == runnerId &&
                         rc.InstanceName == instanceName)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Deletes the active connection for a specific runner instance.
    /// Used when a runner disconnects.
    /// </summary>
    public async Task DeleteConnection(Guid organizationId, Guid runnerId, string instanceName)
    {
        var connection = await GetActiveConnection(organizationId, runnerId, instanceName);
        if (connection != null)
        {
            DbContext.RunnerConnections.Remove(connection);
            await DbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Deletes all connections owned by a specific server instance.
    /// Used by cleanup job when a server is detected as crashed/offline.
    /// </summary>
    public async Task DeleteConnectionsByServerId(Guid serverInstanceId)
    {
        var connections = await DbContext.RunnerConnections
            .Where(rc => rc.ServerInstanceId == serverInstanceId)
            .ToListAsync();

        if (connections.Count > 0)
        {
            DbContext.RunnerConnections.RemoveRange(connections);
            await DbContext.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Gets all distinct server instance IDs that have active connections.
    /// Used by cleanup job to determine which servers to check.
    /// </summary>
    public async Task<List<Guid>> GetDistinctServerInstanceIds()
    {
        return await DbContext.RunnerConnections
            .Select(rc => rc.ServerInstanceId)
            .Distinct()
            .ToListAsync();
    }

    /// <summary>
    /// Gets a runner connection by SignalR connection ID.
    /// Used for authorization to map connectionId to runner identity.
    /// </summary>
    public async Task<RunnerConnection?> GetBySignalRConnectionIdAsync(string signalRConnectionId, Guid organizationId)
    {
        return await DbContext.RunnerConnections
            .Include(rc => rc.Runner)
            .FirstOrDefaultAsync(rc => rc.SignalRConnectionId == signalRConnectionId && rc.OrganizationId == organizationId);
    }

    /// <summary>
    /// Gets all active connections for a specific runner (all instances).
    /// Used for runner selection when choosing between multiple instances.
    /// </summary>
    public async Task<List<RunnerConnection>> GetActiveConnectionsByRunnerId(Guid runnerId, Guid organizationId)
    {
        return await DbContext.RunnerConnections
            .Include(rc => rc.Runner)
            .Where(rc => rc.OrganizationId == organizationId && rc.RunnerId == runnerId)
            .ToListAsync();
    }

    /// <summary>
    /// Gets the ids of runners that have at least one active connection in the organization.
    /// Used by the dashboard to compute connected counts in one query instead of one per runner.
    /// </summary>
    public async Task<HashSet<Guid>> GetConnectedRunnerIds(Guid organizationId)
    {
        var runnerIds = await DbContext.RunnerConnections
            .Where(rc => rc.OrganizationId == organizationId)
            .Select(rc => rc.RunnerId)
            .Distinct()
            .ToListAsync();
        return runnerIds.ToHashSet();
    }

    /// <summary>
    /// Gets all connections owned by a specific server instance.
    /// Used by heartbeat consumer to check active connections.
    /// </summary>
    public async Task<List<RunnerConnection>> GetConnectionsByServerInstanceId(Guid serverInstanceId)
    {
        return await DbContext.RunnerConnections
            .Where(rc => rc.ServerInstanceId == serverInstanceId)
            .ToListAsync();
    }

    /// <summary>
    /// Gets the active connection for a specific runner (any instance).
    /// If multiple instances exist, returns the first one found.
    /// Used by consumers to find the SignalR connectionId for sending messages to a runner.
    /// </summary>
    public async Task<RunnerConnection?> GetActiveConnectionByRunnerId(Guid organizationId, Guid runnerId)
    {
        return await DbContext.RunnerConnections
            .Where(rc => rc.OrganizationId == organizationId && rc.RunnerId == runnerId)
            .FirstOrDefaultAsync();
    }
}
