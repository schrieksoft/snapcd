// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Contracts;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas;
using SnapCd.Server.Core.Services.Crud.Jobs;

namespace SnapCd.Server.Core.StateMachine.Gatekeeping.Activities;

public class DequeueModuleJobActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga, TMessage>
    where TSaga : ModuleSaga
    where TMessage : class
{
    private readonly JobService _executionService;
    private readonly ILogger<DequeueModuleJobActivity<TSaga, TMessage>> _logger;
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;

    public DequeueModuleJobActivity(JobService executionService, ILogger<DequeueModuleJobActivity<TSaga, TMessage>> logger, IDbContextFactory<SnapCdDbContext> dbContextFactory)
    {
        _executionService = executionService;
        _logger = logger;
        _dbContextFactory = dbContextFactory;
    }

    public async Task Execute(
        BehaviorContext<TSaga, TMessage> context,
        IBehavior<TSaga, TMessage> next)
    {
        try
        {
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            // Check if there's a current ModuleJob for this module
            var hasCurrentJob = await dbContext.ModuleJobs
                .Where(j => j.ModuleId == context.Saga.CorrelationId && j.OrganizationId == context.Saga.OrganizationId && j.IsCurrent == true)
                .AnyAsync();

            if (!hasCurrentJob && context.Saga.QueuedDesiredStateHeadline.HasValue)
            {
                context.Saga.DesiredStateHeadline = context.Saga.QueuedDesiredStateHeadline.Value;
                context.Saga.QueuedDesiredStateHeadline = null;
                context.Saga.QueuedReason = null;

                if (context.Saga.DesiredStateHeadline == DesiredStateHeadline.Applied)
                {
                    await _executionService.Apply(context.Saga.CorrelationId, context.Saga.OrganizationId);
                    Console.WriteLine($"Dequeued and running Apply for module {context.Saga.CorrelationId}");
                }
                else if (context.Saga.DesiredStateHeadline == DesiredStateHeadline.Destroyed)
                {
                    await _executionService.Destroy(context.Saga.CorrelationId, context.Saga.OrganizationId);
                    Console.WriteLine($"Dequeued and running Destroy for module {context.Saga.CorrelationId}");
                }
            }
            else if (!hasCurrentJob)
            {
                // No current job and no queued requests, clear NextJobId
                Console.WriteLine($"No current job and no queued requests for module {context.Saga.CorrelationId}");
            }
            else
            {
                // There's still a current job, do nothing
                Console.WriteLine($"Current job still running for module {context.Saga.CorrelationId}, not dequeuing");
            }

            // Proceed to the next activity
            await next.Execute(context).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing DequeueModuleJobActivity for {context.Saga.CorrelationId}. Error: {ex.Message}");
            // Still proceed to next activity even on error
            await next.Execute(context).ConfigureAwait(false);
        }
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TSaga, TMessage, TException> context,
        IBehavior<TSaga, TMessage> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("dequeue-module-job");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}