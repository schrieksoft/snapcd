// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.


using MassTransit;
using Microsoft.AspNetCore.SignalR;
using SnapCd.Contracts.Endpoints;
using SnapCd.Server.Core.Events.Steps.RippleProbe;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Services;

namespace SnapCd.Server.Core.Consumers.Tasks.RippleProbe;

/// <summary>Dispatches the probe itself: the one step with no shared runner task behind it.</summary>
public class RippleProbeProbeConsumer : IConsumer<RippleProbeProbeRequested>
{
    private readonly ILogger<RippleProbeProbeConsumer> _logger;
    private readonly IHubContext<RunnerHub> _hubContext;
    private readonly RunnerSelectionService _runnerSelection;

    public RippleProbeProbeConsumer(
        ILogger<RippleProbeProbeConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection)
    {
        _logger = logger;
        _hubContext = hubContext;
        _runnerSelection = runnerSelection;
    }

    public async Task Consume(ConsumeContext<RippleProbeProbeRequested> context)
    {
        var msg = context.Message;
        var jobId = msg.CorrelationId;
        var orgId = msg.OrganizationId;

        try
        {
            var runner = await _runnerSelection.SelectSpecificRunnerAsync(orgId, msg.RunnerId, msg.RunnerInstanceName);

            if (runner == null)
                throw new InvalidOperationException($"No available runners in pool {msg.RunnerId}");

            await _hubContext.Clients.Client(runner.SignalRConnectionId).SendAsync(
                nameof(IRippleProbeEndpoints.RippleProbeProbe),
                RippleProbeStepRequestBuilders.Probe(jobId, orgId, msg.Declared, msg.InputsJson));

            _logger.LogDebug("Dispatched RippleProbeProbe request to runner {RunnerName} for job {JobId}",
                runner.InstanceName, jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching RippleProbeProbe request for job {JobId}", jobId);

            await context.Publish(new RippleProbeProbeFaulted
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
