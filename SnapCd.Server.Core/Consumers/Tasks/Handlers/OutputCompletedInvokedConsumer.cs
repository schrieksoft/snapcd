using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Events.Handlers;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Services.Crud;

namespace SnapCd.Server.Core.Consumers.Tasks.Handlers;

/// <summary>
/// Handles database work for OutputHandler.Complete() invocations.
/// This consumer processes OutputCompletedInvoked events to store output sets
/// without blocking the SignalR connection.
/// </summary>
public class OutputCompletedInvokedConsumer : IConsumer<OutputCompletedInvoked>
{
    private readonly ILogger<OutputCompletedInvokedConsumer> _logger;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly OutputSetService _outputSetService;
    private readonly IBus _bus;

    public OutputCompletedInvokedConsumer(
        ILogger<OutputCompletedInvokedConsumer> logger,
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        OutputSetService outputSetService, IBus bus)
    {
        _logger = logger;
        _dbContextFactory = dbContextFactory;
        _outputSetService = outputSetService;
        _bus = bus;
    }

    public async Task Consume(ConsumeContext<OutputCompletedInvoked> context)
    {
        var jobId = context.Message.JobId;
        var outputSet = context.Message.OutputSet;

        if (outputSet == null)
        {
            _logger.LogDebug("No output set provided for job {JobId}, skipping storage", jobId);
            return;
        }

        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();
            var jobView = await dbContext.ModuleJobs
                .Where(x => x.Id == jobId)
                .Select(x => new { x.OrganizationId, x.ModuleId })
                .FirstOrDefaultAsync();

            if (jobView != null)
            {
                await _outputSetService.CreateWithOutputsNonsecured(outputSet, jobView.ModuleId, jobView.OrganizationId);
                _logger.LogInformation("Stored OutputSet for job {JobId}", jobId);
                
                // Publish saga event
                await _bus.Publish(new OutputCompleted
                {
                    CorrelationId = jobId
                });

                _logger.LogInformation("Output completion processed for job {JobId}", jobId);
            }
            else
            {
                _logger.LogWarning("Could not find module job {JobId} to store OutputSet", jobId);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to store OutputSet for job {JobId}", jobId);
            
            await context.Publish(new OutputFaulted
            {
                CorrelationId = jobId,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                IsServerSideError = true
            });
        }
    }
}
