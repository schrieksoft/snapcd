using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.Handlers;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Services.Crud;

namespace SnapCd.Server.Core.Consumers.Tasks.Handlers;

/// <summary>
/// Handles database work for VariableHandler.Complete() invocations.
/// This consumer processes VariablesCompletedInvoked events to store variable sets
/// without blocking the SignalR connection.
/// </summary>
public class VariablesCompletedInvokedConsumer : IConsumer<VariablesCompletedInvoked>
{
    private readonly ILogger<VariablesCompletedInvokedConsumer> _logger;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly VariableSetService _variableSetService;
    private readonly IBus _bus;

    public VariablesCompletedInvokedConsumer(
        ILogger<VariablesCompletedInvokedConsumer> logger,
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        VariableSetService variableSetService, IBus bus)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _variableSetService = variableSetService;
        _bus = bus;
    }

    public async Task Consume(ConsumeContext<VariablesCompletedInvoked> context)
    {
        var jobId = context.Message.JobId;
        var variableSet = context.Message.VariableSet;

        try
        {
            if (variableSet != null)
            {
                await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
                var jobView = await dbContext.ModuleJobs
                    .Where(x => x.Id == jobId)
                    .Select(x => new { x.OrganizationId, x.ModuleId })
                    .FirstOrDefaultAsync();

                if (jobView != null)
                {
                    await _variableSetService.CreateWithVariables(variableSet, jobView.ModuleId, jobView.OrganizationId);
                    _logger.LogInformation("Stored VariableSet for job {JobId}", jobId);
                }
                else
                {
                    _logger.LogWarning("Could not find module job {JobId} to store VariableSet", jobId);
                }
            }
            else
            {
                _logger.LogDebug("No variable set provided for job {JobId}, skipping storage", jobId);
            }

            await _bus.Publish(new VariablesCompleted
            {
                CorrelationId = jobId
            });

            _logger.LogInformation("Variables completion processed for job {JobId}", jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store VariableSet for job {JobId}", jobId);

            await context.Publish(new VariablesFaulted
            {
                CorrelationId = jobId,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                IsServerSideError = true
            });
        }
    }
}
