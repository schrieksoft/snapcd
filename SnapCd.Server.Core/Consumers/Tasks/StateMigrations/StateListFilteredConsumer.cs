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
using SnapCd.Server.Core.Events.Steps.StateMigrations;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Services;

namespace SnapCd.Server.Core.Consumers.Tasks.StateMigrations;

/// <summary>
/// Sends the filter to the runner. The addresses are the whole request: what else is in the
/// Module's state is not asked for and is not reported.
/// </summary>
public class StateListFilteredConsumer : IConsumer<StateListFilteredRequested>
{
    private readonly ILogger<StateListFilteredConsumer> _logger;
    private readonly IHubContext<RunnerHub> _hubContext;
    private readonly RunnerSelectionService _runnerSelection;

    public StateListFilteredConsumer(
        ILogger<StateListFilteredConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection)
    {
        _logger = logger;
        _hubContext = hubContext;
        _runnerSelection = runnerSelection;
    }

    public async Task Consume(ConsumeContext<StateListFilteredRequested> context)
    {
        var msg = context.Message;
        var jobId = msg.CorrelationId;

        try
        {
            var runner = await _runnerSelection.SelectSpecificRunnerAsync(
                msg.OrganizationId, msg.RunnerId, msg.RunnerInstanceName);

            if (runner == null)
                throw new InvalidOperationException($"No available runners in pool {msg.RunnerId}");

            var ordinary = StepRequestBuilders.Init(jobId, msg.OrganizationId, msg.Declared, []);

            await _hubContext.Clients.Client(runner.SignalRConnectionId).SendAsync(
                RunnerEndpoints.StateListFiltered,
                new StateListFilteredRequestBase
                {
                    JobId = jobId,
                    OrganizationId = msg.OrganizationId,
                    Metadata = ordinary.Metadata,
                    Engine = ordinary.Engine,
                    Addresses = msg.Addresses
                });

            _logger.LogDebug(
                "Dispatched StateListFiltered to runner {RunnerName} for job {JobId}",
                runner.InstanceName, jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching StateListFiltered for job {JobId}", jobId);

            await context.Publish(new StateListFilteredFaulted
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
