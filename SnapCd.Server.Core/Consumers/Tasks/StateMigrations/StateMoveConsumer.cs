// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using Microsoft.AspNetCore.SignalR;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.RunnerRequests.StateMigrations;
using SnapCd.Server.Core.Consumers.Tasks.Builders;
using SnapCd.Server.Core.Enums;
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Services;

namespace SnapCd.Server.Core.Consumers.Tasks.StateMigrations;

/// <summary>Sends the addresses to move to the runner this job is pinned to.</summary>
public class MoveConsumer(
    ILogger<MoveConsumer> logger,
    IHubContext<RunnerHub> hubContext,
    RunnerSelectionService runnerSelection) : IConsumer<MoveRequested>
{
    public async Task Consume(ConsumeContext<MoveRequested> context)
    {
        var msg = context.Message;
        var jobId = msg.CorrelationId;

        try
        {
            var runner = await runnerSelection.SelectSpecificRunnerAsync(
                msg.OrganizationId, msg.RunnerId, msg.RunnerInstanceName);

            if (runner == null)
                throw new InvalidOperationException(
                    $"No runner instance '{msg.RunnerInstanceName}' available in pool {msg.RunnerId}");

            var ordinary = StepRequestBuilders.Init(jobId, msg.OrganizationId, msg.Declared, []);

            await hubContext.Clients.Client(runner.SignalRConnectionId).SendAsync(
                RunnerEndpoints.StateMove,
                new StateMoveRequestBase
                {
                    JobId = jobId,
                    OrganizationId = msg.OrganizationId,
                    Metadata = ordinary.Metadata,
                    Engine = ordinary.Engine,
                    Instructions = msg.Instructions
                        .Select(i => new StateAddressInstruction { Address = i.Address, Target = i.Target })
                        .ToList()
                });

            logger.LogDebug(
                "Sent StateMove to runner {RunnerName} for job {JobId}",
                runner.InstanceName, jobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error dispatching StateMove for job {JobId}", jobId);

            await context.Publish(new MoveFaulted
            {
                CorrelationId = jobId,
                OrganizationId = msg.OrganizationId,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                IsServerSideError = true
            });
        }
    }
}

/// <summary>Sends the addresses to import to the runner this job is pinned to.</summary>
public class ImportConsumer(
    ILogger<ImportConsumer> logger,
    IHubContext<RunnerHub> hubContext,
    RunnerSelectionService runnerSelection) : IConsumer<ImportRequested>
{
    public async Task Consume(ConsumeContext<ImportRequested> context)
    {
        var msg = context.Message;
        var jobId = msg.CorrelationId;

        try
        {
            var runner = await runnerSelection.SelectSpecificRunnerAsync(
                msg.OrganizationId, msg.RunnerId, msg.RunnerInstanceName);

            if (runner == null)
                throw new InvalidOperationException(
                    $"No runner instance '{msg.RunnerInstanceName}' available in pool {msg.RunnerId}");

            var ordinary = StepRequestBuilders.Init(jobId, msg.OrganizationId, msg.Declared, []);

            await hubContext.Clients.Client(runner.SignalRConnectionId).SendAsync(
                RunnerEndpoints.StateImport,
                new StateMoveRequestBase
                {
                    JobId = jobId,
                    OrganizationId = msg.OrganizationId,
                    Metadata = ordinary.Metadata,
                    Engine = ordinary.Engine,
                    Instructions = msg.Instructions
                        .Select(i => new StateAddressInstruction { Address = i.Address, Target = i.Target })
                        .ToList()
                });

            logger.LogDebug(
                "Sent StateImport to runner {RunnerName} for job {JobId}",
                runner.InstanceName, jobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error dispatching StateImport for job {JobId}", jobId);

            await context.Publish(new ImportFaulted
            {
                CorrelationId = jobId,
                OrganizationId = msg.OrganizationId,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                IsServerSideError = true
            });
        }
    }
}

/// <summary>Sends the addresses to remove to the runner this job is pinned to.</summary>
public class RemoveConsumer(
    ILogger<RemoveConsumer> logger,
    IHubContext<RunnerHub> hubContext,
    RunnerSelectionService runnerSelection) : IConsumer<RemoveRequested>
{
    public async Task Consume(ConsumeContext<RemoveRequested> context)
    {
        var msg = context.Message;
        var jobId = msg.CorrelationId;

        try
        {
            var runner = await runnerSelection.SelectSpecificRunnerAsync(
                msg.OrganizationId, msg.RunnerId, msg.RunnerInstanceName);

            if (runner == null)
                throw new InvalidOperationException(
                    $"No runner instance '{msg.RunnerInstanceName}' available in pool {msg.RunnerId}");

            var ordinary = StepRequestBuilders.Init(jobId, msg.OrganizationId, msg.Declared, []);

            await hubContext.Clients.Client(runner.SignalRConnectionId).SendAsync(
                RunnerEndpoints.StateRemove,
                new StateMoveRequestBase
                {
                    JobId = jobId,
                    OrganizationId = msg.OrganizationId,
                    Metadata = ordinary.Metadata,
                    Engine = ordinary.Engine,
                    Instructions = msg.Instructions
                        .Select(i => new StateAddressInstruction { Address = i.Address, Target = i.Target })
                        .ToList()
                });

            logger.LogDebug(
                "Sent StateRemove to runner {RunnerName} for job {JobId}",
                runner.InstanceName, jobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error dispatching StateRemove for job {JobId}", jobId);

            await context.Publish(new RemoveFaulted
            {
                CorrelationId = jobId,
                OrganizationId = msg.OrganizationId,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                IsServerSideError = true
            });
        }
    }
}

/// <summary>Asks the runner what a move would do, before anyone approves it.</summary>
public class MoveDryRunConsumer(
    ILogger<MoveDryRunConsumer> logger,
    IHubContext<RunnerHub> hubContext,
    RunnerSelectionService runnerSelection) : IConsumer<MoveDryRunRequested>
{
    public async Task Consume(ConsumeContext<MoveDryRunRequested> context)
    {
        var msg = context.Message;
        var jobId = msg.CorrelationId;

        try
        {
            var runner = await runnerSelection.SelectSpecificRunnerAsync(
                msg.OrganizationId, msg.RunnerId, msg.RunnerInstanceName);

            if (runner == null)
                throw new InvalidOperationException(
                    $"No runner instance '{msg.RunnerInstanceName}' available in pool {msg.RunnerId}");

            var ordinary = StepRequestBuilders.Init(jobId, msg.OrganizationId, msg.Declared, []);

            await hubContext.Clients.Client(runner.SignalRConnectionId).SendAsync(
                RunnerEndpoints.MoveDryRun,
                new StateMoveRequestBase
                {
                    JobId = jobId,
                    OrganizationId = msg.OrganizationId,
                    Metadata = ordinary.Metadata,
                    Engine = ordinary.Engine,
                    Instructions = msg.Instructions
                        .Select(i => new StateAddressInstruction { Address = i.Address, Target = i.Target })
                        .ToList()
                });

            logger.LogDebug(
                "Sent MoveDryRun to runner {RunnerName} for job {JobId}",
                runner.InstanceName, jobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error dispatching MoveDryRun for job {JobId}", jobId);

            await context.Publish(new MoveDryRunFaulted
            {
                CorrelationId = jobId,
                OrganizationId = msg.OrganizationId,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                IsServerSideError = true
            });
        }
    }
}

/// <summary>Asks the runner what a remove would take out, before anyone approves it.</summary>
public class RemoveDryRunConsumer(
    ILogger<RemoveDryRunConsumer> logger,
    IHubContext<RunnerHub> hubContext,
    RunnerSelectionService runnerSelection) : IConsumer<RemoveDryRunRequested>
{
    public async Task Consume(ConsumeContext<RemoveDryRunRequested> context)
    {
        var msg = context.Message;
        var jobId = msg.CorrelationId;

        try
        {
            var runner = await runnerSelection.SelectSpecificRunnerAsync(
                msg.OrganizationId, msg.RunnerId, msg.RunnerInstanceName);

            if (runner == null)
                throw new InvalidOperationException(
                    $"No runner instance '{msg.RunnerInstanceName}' available in pool {msg.RunnerId}");

            var ordinary = StepRequestBuilders.Init(jobId, msg.OrganizationId, msg.Declared, []);

            await hubContext.Clients.Client(runner.SignalRConnectionId).SendAsync(
                RunnerEndpoints.RemoveDryRun,
                new StateMoveRequestBase
                {
                    JobId = jobId,
                    OrganizationId = msg.OrganizationId,
                    Metadata = ordinary.Metadata,
                    Engine = ordinary.Engine,
                    Instructions = msg.Instructions
                        .Select(i => new StateAddressInstruction { Address = i.Address, Target = i.Target })
                        .ToList()
                });

            logger.LogDebug(
                "Sent RemoveDryRun to runner {RunnerName} for job {JobId}",
                runner.InstanceName, jobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error dispatching RemoveDryRun for job {JobId}", jobId);

            await context.Publish(new RemoveDryRunFaulted
            {
                CorrelationId = jobId,
                OrganizationId = msg.OrganizationId,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                IsServerSideError = true
            });
        }
    }
}

/// <summary>Asks the runner whether the addresses are free to import onto.</summary>
public class ImportPreCheckConsumer(
    ILogger<ImportPreCheckConsumer> logger,
    IHubContext<RunnerHub> hubContext,
    RunnerSelectionService runnerSelection) : IConsumer<ImportPreCheckRequested>
{
    public async Task Consume(ConsumeContext<ImportPreCheckRequested> context)
    {
        var msg = context.Message;
        var jobId = msg.CorrelationId;

        try
        {
            var runner = await runnerSelection.SelectSpecificRunnerAsync(
                msg.OrganizationId, msg.RunnerId, msg.RunnerInstanceName);

            if (runner == null)
                throw new InvalidOperationException(
                    $"No runner instance '{msg.RunnerInstanceName}' available in pool {msg.RunnerId}");

            var ordinary = StepRequestBuilders.Init(jobId, msg.OrganizationId, msg.Declared, []);

            await hubContext.Clients.Client(runner.SignalRConnectionId).SendAsync(
                RunnerEndpoints.ImportPreCheck,
                new StateMoveRequestBase
                {
                    JobId = jobId,
                    OrganizationId = msg.OrganizationId,
                    Metadata = ordinary.Metadata,
                    Engine = ordinary.Engine,
                    Instructions = msg.Instructions
                        .Select(i => new StateAddressInstruction { Address = i.Address, Target = i.Target })
                        .ToList()
                });

            logger.LogDebug(
                "Sent ImportPreCheck to runner {RunnerName} for job {JobId}",
                runner.InstanceName, jobId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error dispatching ImportPreCheck for job {JobId}", jobId);

            await context.Publish(new ImportPreCheckFaulted
            {
                CorrelationId = jobId,
                OrganizationId = msg.OrganizationId,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                IsServerSideError = true
            });
        }
    }
}
