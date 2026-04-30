using MassTransit;
using Microsoft.AspNetCore.SignalR;
using SnapCd.Contracts;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Services;

namespace SnapCd.Server.Core.Consumers.Tasks;

/// <summary>
/// Server-side consumer that receives Output requests and dispatches them to runners via SignalR.
/// Replaces the old runner-side consumer pattern with direct hub invocation.
/// </summary>
public class OutputConsumer : IConsumer<OutputRequested>
{
    private readonly ILogger<OutputConsumer> _logger;
    private readonly IHubContext<RunnerHub> _hubContext;
    private readonly RunnerSelectionService _runnerSelection;

    public OutputConsumer(
        ILogger<OutputConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection)
    {
        _logger = logger;
        _hubContext = hubContext;
        _runnerSelection = runnerSelection;
    }

    public async Task Consume(ConsumeContext<OutputRequested> context)
    {
        var msg = context.Message;
        var jobId = msg.CorrelationId;

        var orgId = msg.OrganizationId;
        var runnerId = msg.RunnerId;
        var specificRunner = msg.RunnerInstanceName;

        _logger.LogInformation("Received Output request for job {JobId} in pool {RunnerId}",
            jobId, runnerId);

        try
        {
            var runner = await _runnerSelection.SelectSpecificRunnerAsync(orgId, runnerId, specificRunner);

            if (runner == null)
            {
                _logger.LogWarning("No available runners in pool {RunnerId} for job {JobId}", runnerId, jobId);
                throw new InvalidOperationException($"No available runners in pool {runnerId}");
            }

            _logger.LogInformation("Selected runner {RunnerName} (ConnectionId: {ConnectionId}) for job {JobId}",
                runner.InstanceName, runner.SignalRConnectionId, jobId);


            // Invoke method on specific runner via SignalR
            await _hubContext.Clients.Client(runner.SignalRConnectionId).SendAsync(
                RunnerEndpoints.Output,
                new OutputRequestBase
                {
                    JobId = jobId,
                    OrganizationId = orgId,
                    Metadata = new JobMetadata
                    {
                        ModuleName = msg.Declared.ModuleName,
                        NamespaceName = msg.Declared.NamespaceName,
                        StackName = msg.Declared.StackName,
                        ModuleId = msg.Declared.ModuleId,
                        SourceSubdirectory = msg.Declared.SourceSubdirectory
                    },
                    Engine = msg.Declared.Engine,
                    OutputBeforeHook = msg.Declared.OutputBeforeHook,
                    OutputAfterHook = msg.Declared.OutputAfterHook,
                    ExtraFileNames = msg.Declared.ExtraFiles?.Select(f => f.FileName).ToList(),
                    PulumiFlags = msg.Declared.PulumiFlags
                        .Where(f => f.Task == PulumiCommandTask.Output)
                        .ToList(),
                    PulumiArrayFlags = msg.Declared.PulumiArrayFlags
                        .Where(f => f.Task == PulumiCommandTask.Output)
                        .ToList(),
                    TerraformFlags = msg.Declared.TerraformFlags
                        .Where(f => f.Task == TerraformCommandTask.Output)
                        .ToList(),
                    TerraformArrayFlags = msg.Declared.TerraformArrayFlags
                        .Where(f => f.Task == TerraformCommandTask.Output)
                        .ToList()
                }
            );

            _logger.LogInformation("Dispatched Output request to runner {RunnerName} for job {JobId}",
                runner.InstanceName, jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching Output request for job {JobId}", jobId);
            await context.Publish(new OutputFaulted
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