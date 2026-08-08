// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;
using SnapCd.Server.Core.Settings;

namespace SnapCd.Server.Core.Services;

/// <summary>
/// Thrown when a Runner has registered connections but none can be dispatched to from
/// this server. Distinct from "no runner is connected at all": it means the rows exist
/// but point at a server instance that is not this one — normally a leftover from a
/// server that died without cleaning up.
/// </summary>
public class RunnerUnreachableException : InvalidOperationException
{
    public RunnerUnreachableException(string message) : base(message)
    {
    }
}

/// <summary>
/// Selects the optimal runner from available connections for job assignment.
/// Uses least-loaded strategy based on active job counts.
/// </summary>
public class RunnerSelectionService
{
    private readonly RunnerConnectionRepositoryFactory _connectionRepositoryFactory;
    private readonly SnapCdDbContext _dbContext;
    private readonly ServerSettings _serverSettings;
    private readonly ILogger<RunnerSelectionService> _logger;

    public RunnerSelectionService(
        RunnerConnectionRepositoryFactory connectionRepositoryFactory,
        SnapCdDbContext dbContext,
        IOptions<ServerSettings> serverSettings,
        ILogger<RunnerSelectionService> logger)
    {
        _connectionRepositoryFactory = connectionRepositoryFactory;
        _dbContext = dbContext;
        _serverSettings = serverSettings.Value;
        _logger = logger;
    }

    /// <summary>
    /// A RunnerConnection row is only dispatchable from the server that owns its SignalR
    /// connection. A row owned by another ServerInstanceId is either held by a peer (which
    /// should dispatch it instead) or is a leftover from a server that died without
    /// cleaning up. Either way this server cannot reach it: SignalR's
    /// <c>Clients.Client(unknownId)</c> silently no-ops, so dispatching to it would look
    /// like success and then time out with no logs. Filtering here makes the caller's
    /// "no available runners" path fire instead — loudly, and immediately.
    /// </summary>
    private bool IsDispatchableFromThisServer(RunnerConnection connection)
    {
        if (connection.ServerInstanceId == _serverSettings.InstanceId)
            return true;

        _logger.LogWarning(
            "Ignoring runner connection {InstanceName} (runner {RunnerId}): it is registered to server "
            + "{OwningServerInstanceId}, not this server ({ServerInstanceId}). If that server is gone, "
            + "ServerConnectionCleanupJob will remove the stale connection.",
            connection.InstanceName,
            connection.RunnerId,
            connection.ServerInstanceId,
            _serverSettings.InstanceId);

        return false;
    }


    public async Task<RunnerConnection?> SelectSpecificRunnerAsync(
        Guid organizationId,
        Guid runnerId,
        string specificRunnerName)
    {
        _logger.LogDebug("Attempting to select specific runner {RunnerName} for runner {RunnerId}",
            specificRunnerName, runnerId);

        using var connectionRepository = _connectionRepositoryFactory.Create();
        var connection = await connectionRepository.GetActiveConnection(organizationId, runnerId, specificRunnerName);

        if (connection == null)
        {
            _logger.LogWarning("Specific runner {RunnerName} not found for runner {RunnerId}",
                specificRunnerName, runnerId);
            return null;
        }

        if (!IsDispatchableFromThisServer(connection))
            throw new RunnerUnreachableException(
                $"Runner instance '{specificRunnerName}' is registered to a different server instance "
                + $"({connection.ServerInstanceId}) and cannot be reached from this one. If that server is no "
                + "longer running, this is a stale connection and will be cleaned up automatically; retry the job "
                + "once it clears.");

        return connection;
    }
    
    
    /// <summary>
    /// Select a runner instance using least-loaded strategy.
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="runnerId">Runner ID</param>
    /// <returns>Runner connection info, or null if no runner is connected.</returns>
    /// <exception cref="RunnerUnreachableException">
    /// The runner has registered connections, but all of them belong to other server
    /// instances and cannot be dispatched to from here.
    /// </exception>
    public async Task<RunnerConnection?> SelectRunnerInstance(
        Guid organizationId,
        Guid runnerId)
    {

        // Get all available runner connections (all instances of this runner)
        using var connectionRepository = _connectionRepositoryFactory.Create();
        var allConnections = await connectionRepository.GetActiveConnectionsByRunnerId(runnerId, organizationId);

        // Drop any connection this server cannot actually reach over SignalR.
        var connections = allConnections.Where(IsDispatchableFromThisServer).ToList();

        if (connections.Count == 0)
        {
            if (allConnections.Count > 0)
            {
                _logger.LogWarning(
                    "Runner {RunnerId} has {StaleCount} registered connection(s) in organization {OrgId}, but none are "
                    + "reachable from this server. They belong to other server instances — stale rows if those servers "
                    + "are gone.",
                    runnerId, allConnections.Count, organizationId);

                throw new RunnerUnreachableException(
                    $"Runner has {allConnections.Count} registered connection(s), but none can be reached from this "
                    + "server — they are registered to other server instances "
                    + $"({string.Join(", ", allConnections.Select(c => c.ServerInstanceId).Distinct())}). If those "
                    + "servers are no longer running, these are stale connections and will be cleaned up "
                    + "automatically; retry the job once they clear.");
            }

            _logger.LogWarning("No runner instances available for runner {RunnerId} in organization {OrgId}",
                runnerId, organizationId);

            return null;
        }

        _logger.LogDebug("Found {Count} available runner instance(s) for runner {RunnerId}",
            connections.Count, runnerId);

        // If only one instance, return it immediately without querying job counts
        if (connections.Count == 1)
        {
            var singleConnection = connections[0];
            _logger.LogDebug("Only one runner instance available, selecting {InstanceName}",
                singleConnection.InstanceName);
            return singleConnection;
        }

        // Multiple instances available - use least-loaded strategy
        // Query database for active jobs per runner instance
        var jobCounts = await GetActiveJobCountsByRunnerAsync(organizationId, runnerId);

        // Select runner instance with fewest active jobs
        var selectedConnection = connections
            .OrderBy(r => jobCounts.GetValueOrDefault(r.InstanceName, 0))
            .ThenBy(r => r.CreatedDateTime) // Tie-breaker: oldest connection first
            .First();

        var jobCount = jobCounts.GetValueOrDefault(selectedConnection.InstanceName, 0);
        _logger.LogDebug("Selected runner instance {InstanceName} with {JobCount} active jobs (from {TotalInstances} available)",
            selectedConnection.InstanceName, jobCount, connections.Count);

        return selectedConnection;
    }

    /// <summary>
    /// Query the database saga stores to count active jobs per runner.
    /// Queries ApplyJobSaga and DestroyJobSaga tables.
    /// </summary>
    private async Task<Dictionary<string, int>> GetActiveJobCountsByRunnerAsync(Guid organizationId, Guid runnerId)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        // Terminal states that should not be counted as "active"
        // var terminalStates = new[] { "Completed", "Faulted", "Failed", "Cancelled" };
        
        var terminalStateEnums = new[] { ModuleJobSagaState.Completed, ModuleJobSagaState.Faulted, ModuleJobSagaState.Failed, ModuleJobSagaState.Cancelled };
        var terminalStates = terminalStateEnums.Select( x => x.ToString()).ToList();
        
        try
        {
            // Query ApplyJobSaga for active jobs
            var applyJobCounts = await _dbContext.ApplyJobSagas
                .Where(s => s.OrganizationId == organizationId
                            && s.RunnerId == runnerId
                            && !terminalStates.Contains(s.CurrentState)
                            && !string.IsNullOrEmpty(s.RunnerInstanceName))
                .GroupBy(s => s.RunnerInstanceName)
                .Select(g => new { RunnerName = g.Key!, Count = g.Count() })
                .ToListAsync();

            // Query DestroyJobSaga for active jobs
            var destroyJobCounts = await _dbContext.DestroyJobSagas
                .Where(s => s.OrganizationId == organizationId
                            && s.RunnerId == runnerId
                            && !terminalStates.Contains(s.CurrentState)
                            && !string.IsNullOrEmpty(s.RunnerInstanceName))
                .GroupBy(s => s.RunnerInstanceName)
                .Select(g => new { RunnerName = g.Key!, Count = g.Count() })
                .ToListAsync();

            // Aggregate counts
            foreach (var item in applyJobCounts) counts[item.RunnerName] = counts.GetValueOrDefault(item.RunnerName, 0) + item.Count;

            foreach (var item in destroyJobCounts) counts[item.RunnerName] = counts.GetValueOrDefault(item.RunnerName, 0) + item.Count;

            _logger.LogDebug("Active job counts: {Counts}", string.Join(", ", counts.Select(kvp => $"{kvp.Key}={kvp.Value}")));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying active job counts, returning empty counts");
            // Return empty counts - will default to round-robin by connection time
        }

        return counts;
    }
}