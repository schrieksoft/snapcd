using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Definition;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

namespace SnapCd.Server.Core.Services;

/// <summary>
/// Selects the optimal runner from available connections for job assignment.
/// Uses least-loaded strategy based on active job counts.
/// </summary>
public class RunnerSelectionService
{
    private readonly RunnerConnectionRepositoryFactory _connectionRepositoryFactory;
    private readonly SnapCdDbContext _dbContext;
    private readonly ILogger<RunnerSelectionService> _logger;

    public RunnerSelectionService(
        RunnerConnectionRepositoryFactory connectionRepositoryFactory,
        SnapCdDbContext dbContext,
        ILogger<RunnerSelectionService> logger)
    {
        _connectionRepositoryFactory = connectionRepositoryFactory;
        _dbContext = dbContext;
        _logger = logger;
    }


    public async Task<RunnerConnection?> SelectSpecificRunnerAsync(
        Guid organizationId,
        Guid runnerId,
        string specificRunnerName)
    {
        _logger.LogInformation("Attempting to select specific runner {RunnerName} for runner {RunnerId}",
            specificRunnerName, runnerId);

        using var connectionRepository = _connectionRepositoryFactory.Create();
        var connection = await connectionRepository.GetActiveConnection(organizationId, runnerId, specificRunnerName);

        if (connection == null)
            _logger.LogWarning("Specific runner {RunnerName} not found for runner {RunnerId}",
                specificRunnerName, runnerId);

        return connection;
    }
    
    
    /// <summary>
    /// Select a runner instance using least-loaded strategy.
    /// If a specific runner name is provided, attempts to select that runner specifically.
    /// </summary>
    /// <param name="organizationId">Organization ID</param>
    /// <param name="runnerId">Runner ID</param>
    /// <param name="specificRunnerName">Optional specific instance name to select</param>
    /// <returns>Runner connection info, or null if no runners available</returns>
    public async Task<RunnerConnection?> SelectRunnerInstance(
        Guid organizationId,
        Guid runnerId)
    {

        // Get all available runner connections (all instances of this runner)
        using var connectionRepository = _connectionRepositoryFactory.Create();
        var connections = await connectionRepository.GetActiveConnectionsByRunnerId(runnerId, organizationId);

        if (connections.Count == 0)
        {
            _logger.LogWarning("No runner instances available for runner {RunnerId} in organization {OrgId}",
                runnerId, organizationId);
            return null;
        }

        _logger.LogInformation("Found {Count} available runner instance(s) for runner {RunnerId}",
            connections.Count, runnerId);

        // If only one instance, return it immediately without querying job counts
        if (connections.Count == 1)
        {
            var singleConnection = connections[0];
            _logger.LogInformation("Only one runner instance available, selecting {InstanceName}",
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
        _logger.LogInformation("Selected runner instance {InstanceName} with {JobCount} active jobs (from {TotalInstances} available)",
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