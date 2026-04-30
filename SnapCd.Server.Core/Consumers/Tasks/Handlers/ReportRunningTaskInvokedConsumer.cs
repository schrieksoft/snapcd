using MassTransit;
using SnapCd.Server.Core.Events.Handlers;
using SnapCd.Server.Core.Repositories.Organizations.Nonsecured;

namespace SnapCd.Server.Core.Consumers.Tasks.Handlers;

/// <summary>
/// Handles database work for ReportRunningTaskHandler.Report() invocations.
/// This consumer processes ReportRunningTaskInvoked events to track running tasks
/// without blocking the SignalR connection.
/// </summary>
public class ReportRunningTaskInvokedConsumer : IConsumer<ReportRunningTaskInvoked>
{
    private readonly ILogger<ReportRunningTaskInvokedConsumer> _logger;
    private readonly RunnerConnectionJobRepositoryFactory _runnerConnectionJobRepositoryFactory;

    public ReportRunningTaskInvokedConsumer(
        ILogger<ReportRunningTaskInvokedConsumer> logger,
        RunnerConnectionJobRepositoryFactory runnerConnectionJobRepositoryFactory)
    {
        _logger = logger;
        _runnerConnectionJobRepositoryFactory = runnerConnectionJobRepositoryFactory;
    }

    public async Task Consume(ConsumeContext<ReportRunningTaskInvoked> context)
    {
        var message = context.Message;

        try
        {
            using var repository = _runnerConnectionJobRepositoryFactory.Create();
            await repository.CreateOrUpdate(
                message.OrganizationId,
                message.JobId,
                message.TaskName,
                message.RunnerId,
                message.RunnerInstanceName);

            _logger.LogInformation(
                "Recorded running task {TaskName} for job {JobId} on runner {RunnerId}",
                message.TaskName, message.JobId, message.RunnerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to record running task {TaskName} for job {JobId}",
                message.TaskName, message.JobId);
            // Don't rethrow - this is tracking information, not critical
        }
    }
}
