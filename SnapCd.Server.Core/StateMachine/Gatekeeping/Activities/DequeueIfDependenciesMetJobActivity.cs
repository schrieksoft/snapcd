using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Services.Crud.Jobs;

namespace SnapCd.Server.Core.StateMachine.Gatekeeping.Activities;

public class DequeueIfDependenciesMetJobActivity<TMessage> :
    IStateMachineActivity<ModuleSaga, TMessage> where TMessage : class

{
    private readonly JobService _executionService;
    private readonly ILogger<DequeueIfDependenciesMetJobActivity<TMessage>> _logger;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;

    public DequeueIfDependenciesMetJobActivity(
        JobService executionService,
        ILogger<DequeueIfDependenciesMetJobActivity<TMessage>> logger,
        IDbContextFactory<SnapCdDbContext> dbContextFactory)
    {
        _executionService = executionService;
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    public async Task Execute(
        BehaviorContext<ModuleSaga, TMessage> context,
        IBehavior<ModuleSaga, TMessage> next)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            // Check if there's a current ModuleJob for this module
            var hasCurrentJob = await dbContext.ModuleJobs
                .Where(j => j.ModuleId == context.Saga.CorrelationId && j.OrganizationId == context.Saga.OrganizationId && j.IsCurrent == true)
                .AnyAsync();

            // Only proceed if there's no current job and there's a queued request
            if (!hasCurrentJob && context.Saga.QueuedDesiredStateHeadline.HasValue)
            {
                // Check dependencies before dequeuing and executing
                var canExecute = await _executionService.CheckDependenciesAsync(
                    context.Saga.CorrelationId,
                    context.Saga.OrganizationId,
                    context.Saga.QueuedDesiredStateHeadline.Value);

                if (canExecute)
                {
                    var hasActiveRunner = await _executionService.CheckRunnerAvailabilityAsync(context.Saga.CorrelationId);
                    if (!hasActiveRunner)
                    {
                        context.Saga.QueuedReason = QueuedReason.WaitingOnRunnerCheckin;
                        Console.WriteLine($"No active runners available for module {context.Saga.CorrelationId}, keeping queued");
                    }
                    else
                    {
                        // Dependencies are met and runner is available, dequeue and execute
                        context.Saga.DesiredStateHeadline = context.Saga.QueuedDesiredStateHeadline.Value;
                        context.Saga.QueuedDesiredStateHeadline = null;
                        context.Saga.QueuedReason = null;

                        if (context.Saga.DesiredStateHeadline == DesiredStateHeadline.Applied)
                        {
                            await _executionService.Apply(context.Saga.CorrelationId, context.Saga.OrganizationId);
                            Console.WriteLine($"Dependencies met and runner available, dequeued and running Apply for module {context.Saga.CorrelationId}");
                        }
                        else if (context.Saga.DesiredStateHeadline == DesiredStateHeadline.Destroyed)
                        {
                            await _executionService.Destroy(context.Saga.CorrelationId, context.Saga.OrganizationId);
                            Console.WriteLine($"Dependencies met and runner available, dequeued and running Destroy for module {context.Saga.CorrelationId}");
                        }
                    }
                }
                else
                {
                    context.Saga.QueuedReason = QueuedReason.WaitingOnDependencies;
                    Console.WriteLine($"Dependencies not yet met for module {context.Saga.CorrelationId}, keeping queued");
                }
            }
            else if (!hasCurrentJob)
            {
                // No current job and no queued requests
                Console.WriteLine($"No current job and no queued requests for module {context.Saga.CorrelationId}");
            }
            else
            {
                // There's still a current job, do nothing
                Console.WriteLine($"Current job still running for module {context.Saga.CorrelationId}, not checking dependencies");
            }

            // Proceed to the next activity
            await next.Execute(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing DequeueIfDependenciesMetJobActivity for {context.Saga.CorrelationId}. Error: {ex.Message}");
            // Still proceed to next activity even on error
            await next.Execute(context).ConfigureAwait(false);
        }
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<ModuleSaga, TMessage, TException> context,
        IBehavior<ModuleSaga, TMessage> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("dequeue-if-dependencies-met-job");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}