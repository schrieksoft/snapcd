using MassTransit;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Services;

namespace SnapCd.Server.Core.Consumers.Tasks;

/// <summary>
/// Server-side consumer that receives initialization requests, selects a runner, and publishes completion.
/// </summary>
public class SelectRunnerInstanceConsumer : IConsumer<SelectRunnerInstanceRequested>
{
    private readonly ILogger<SelectRunnerInstanceConsumer> _logger;
    private readonly RunnerSelectionService _runnerSelection;
    private readonly RunnerJobAuthorizationService _authorizationService;

    public SelectRunnerInstanceConsumer(
        ILogger<SelectRunnerInstanceConsumer> logger,
        RunnerSelectionService runnerSelection,
        RunnerJobAuthorizationService authorizationService)
    {
        _logger = logger;
        _runnerSelection = runnerSelection;
        _authorizationService = authorizationService;
    }

    public async Task Consume(ConsumeContext<SelectRunnerInstanceRequested> context)
    {
        var msg = context.Message;
        var jobId = msg.CorrelationId; // Use CorrelationId as JobId

        // Extract Runner info from Declared
        var orgId = msg.Declared.OrganizationId;
        var runnerId = msg.RunnerId;

        _logger.LogInformation("Received SelectRunnerInstanceRequested for job {JobId} in pool {RunnerId}",
            jobId, runnerId);

        try
        {
            // Validate that the runner has access to this module
            await _authorizationService.ValidateRunnerAssignedToModule(runnerId, msg.Declared.ModuleId, orgId);

            // Select runner using least-loaded strategy
            var runnerConnection = await _runnerSelection.SelectRunnerInstance(orgId, runnerId);

            if (runnerConnection == null)
            {
                _logger.LogWarning("No available connections for runner {RunnerId} for job {JobId}", runnerId, jobId);
                throw new InvalidOperationException($"No available connections for runner {runnerId}");
                // MassTransit will retry this message
            }

            _logger.LogInformation("Selected runner {RunnerName} for job {JobId}",
                runnerConnection.InstanceName, jobId);

            // Directly publish completion with the selected runner name
            await context.Publish(new SelectRunnerInstanceCompleted
            {
                RunnerInstanceName = runnerConnection.InstanceName,
                CorrelationId = jobId
            });

            _logger.LogInformation("Published initialization completed for job {JobId} with runner {RunnerName}",
                jobId, runnerConnection.InstanceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SelectRunnerInstance request for job {JobId}", jobId);
            await context.Publish(new SelectRunnerInstanceFaulted
            {
                CorrelationId = jobId,
                OrganizationId = orgId,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                IsServerSideError = true
            });
        }
    }
}