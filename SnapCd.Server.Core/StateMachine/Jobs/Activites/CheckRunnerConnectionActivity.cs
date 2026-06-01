// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.EntityFrameworkCore;
using SnapCd.Server.Core.Database;
using SnapCd.Server.Core.Entities.Sagas.Base;
using SnapCd.Server.Core.Events.Runners;

namespace SnapCd.Server.Core.StateMachine.Jobs.Activites;

/// <summary>
/// Activity that performs self-healing checks for disconnected runners.
/// Checks immediately, after 5 seconds, and after 15 seconds total.
/// Publishes RunnerReconnectedEvent if runner is found.
/// </summary>
public class CheckRunnerConnectionActivity<TSaga, TMessage> :
    IStateMachineActivity<TSaga>
    where TSaga : JobSagaBase
    where TMessage : class
{
    private readonly IDbContextFactory<SnapCdDbContext> _dbContextFactory;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<CheckRunnerConnectionActivity<TSaga, TMessage>> _logger;

    public CheckRunnerConnectionActivity(
        IDbContextFactory<SnapCdDbContext> dbContextFactory,
        IPublishEndpoint publishEndpoint,
        ILogger<CheckRunnerConnectionActivity<TSaga, TMessage>> logger)
    {
        _dbContextFactory = dbContextFactory;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Execute(
        BehaviorContext<TSaga> context,
        IBehavior<TSaga> next)
    {
        StartSelfHealingChecks(context.Saga);
        await next.Execute(context);
    }

    public async Task Execute<T>(
        BehaviorContext<TSaga, T> context,
        IBehavior<TSaga, T> next)
        where T : class
    {
        StartSelfHealingChecks(context.Saga);
        await next.Execute(context);
    }

    private void StartSelfHealingChecks(TSaga saga)
    {
        _logger.LogInformation(
            "CheckRunnerConnectionActivity: Starting self-healing checks for RunnerId={RunnerId}, InstanceName={InstanceName}",
            saga.RunnerId, saga.RunnerInstanceName);

        // Start background task for self-healing checks
        // Don't await - let it run in background
        _ = Task.Run(async () =>
        {
            try
            {
                await PerformSelfHealingChecks(saga);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "CheckRunnerConnectionActivity: Error in self-healing checks for RunnerId={RunnerId}",
                    saga.RunnerId);
            }
        });
    }

    private async Task PerformSelfHealingChecks(TSaga saga)
    {
        var checkIntervals = new[] { 0, 5000, 10000 }; // 0s, 5s, +10s (total 15s)

        for (int attempt = 0; attempt < checkIntervals.Length; attempt++)
        {
            if (attempt > 0)
            {
                _logger.LogInformation(
                    "CheckRunnerConnectionActivity: Waiting {Delay}ms before check attempt {Attempt}",
                    checkIntervals[attempt], attempt + 1);

                await Task.Delay(checkIntervals[attempt]);
            }

            _logger.LogInformation(
                "CheckRunnerConnectionActivity: Performing check attempt {Attempt} for RunnerId={RunnerId}, InstanceName={InstanceName}",
                attempt + 1, saga.RunnerId, saga.RunnerInstanceName);

            // Create new DbContext for this check (activities can't share contexts across threads)
            await using var dbContext = await _dbContextFactory.CreateDbContextAsync();

            var connection = await dbContext.RunnerConnections
                .FirstOrDefaultAsync(rc =>
                    rc.RunnerId == saga.RunnerId &&
                    rc.OrganizationId == saga.OrganizationId &&
                    rc.InstanceName == saga.RunnerInstanceName);

            if (connection != null)
            {
                _logger.LogInformation(
                    "CheckRunnerConnectionActivity: Runner reconnected! Found on ServerInstanceId={ServerInstanceId}, publishing RunnerReconnectedEvent",
                    connection.ServerInstanceId);

                // Runner is back! Publish event to wake up the saga
                await _publishEndpoint.Publish(new RunnerReconnectedEvent
                {
                    OrganizationId = saga.OrganizationId,
                    RunnerId = saga.RunnerId,
                    InstanceName = saga.RunnerInstanceName!,
                    ServerInstanceId = connection.ServerInstanceId
                });

                return; // Success - stop checking
            }

            _logger.LogInformation(
                "CheckRunnerConnectionActivity: Check attempt {Attempt} - runner still not connected",
                attempt + 1);
        }

        _logger.LogWarning(
            "CheckRunnerConnectionActivity: All self-healing checks exhausted. Runner {RunnerId}/{InstanceName} still not connected. Waiting for external RunnerReconnectedEvent.",
            saga.RunnerId, saga.RunnerInstanceName);
    }

    public Task Faulted<TException>(
        BehaviorExceptionContext<TSaga, TException> context,
        IBehavior<TSaga> next)
        where TException : Exception
    {
        return next.Faulted(context);
    }

    public Task Faulted<T, TException>(
        BehaviorExceptionContext<TSaga, T, TException> context,
        IBehavior<TSaga, T> next)
        where T : class
        where TException : Exception
    {
        return next.Faulted(context);
    }

    public void Probe(ProbeContext context)
    {
        context.CreateScope("check-runner-connection");
    }

    public void Accept(StateMachineVisitor visitor)
    {
        visitor.Visit(this);
    }
}
