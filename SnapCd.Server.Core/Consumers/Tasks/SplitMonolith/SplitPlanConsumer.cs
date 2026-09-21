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
using SnapCd.Server.Core.Consumers.Tasks.Builders;
using SnapCd.Server.Core.Events.Steps.SplitMonolith;
using SnapCd.Server.Core.Factories;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Services;

namespace SnapCd.Server.Core.Consumers.Tasks.SplitMonolith;

/// <summary>
/// Dispatches a split job's plan step. A split never destroys, so the plan is always the apply
/// shape and its parameters resolve the same way an ordinary plan's do.
/// </summary>
public class SplitPlanConsumer : IConsumer<SplitPlanRequested>
{
    private readonly ILogger<SplitPlanConsumer> _logger;
    private readonly IHubContext<RunnerHub> _hubContext;
    private readonly RunnerSelectionService _runnerSelection;
    private readonly ParamResolverFactory _paramResolverFactory;

    public SplitPlanConsumer(
        ILogger<SplitPlanConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection,
        ParamResolverFactory paramResolverFactory)
    {
        _logger = logger;
        _hubContext = hubContext;
        _runnerSelection = runnerSelection;
        _paramResolverFactory = paramResolverFactory;
    }

    public async Task Consume(ConsumeContext<SplitPlanRequested> context)
    {
        var msg = context.Message;
        var jobId = msg.CorrelationId;
        var orgId = msg.OrganizationId;

        try
        {
            var resolvedParameters = await StepRequestBuilders.ResolvePlanParameters(
                _paramResolverFactory, jobId, orgId, msg.Declared, _logger);

            var runner = await _runnerSelection.SelectSpecificRunnerAsync(orgId, msg.RunnerId, msg.RunnerInstanceName);
            if (runner == null)
                throw new InvalidOperationException($"No available runners in pool {msg.RunnerId}");

            await _hubContext.Clients.Client(runner.SignalRConnectionId).SendAsync(
                RunnerEndpoints.Plan,
                StepRequestBuilders.Plan(jobId, orgId, msg.Declared, resolvedParameters, isDestroyJob: false));

            _logger.LogDebug("Dispatched SplitPlan request to runner {RunnerName} for job {JobId}",
                runner.InstanceName, jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching SplitPlan request for job {JobId}", jobId);
            await context.Publish(new SplitPlanFaulted
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
