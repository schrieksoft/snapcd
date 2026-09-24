// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using Microsoft.AspNetCore.SignalR;
using SnapCd.Server.Core.Events.Steps.Transfer;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.Crud.Transfers;

namespace SnapCd.Server.Core.Consumers.Tasks.Transfers;

/// <summary>
/// The dispatch every transfer step shares: check the Module is still party to the transfer, find
/// the runner it was pinned to, send the step to it, and turn anything that goes wrong into the
/// step's own faulted event so the saga hears about it rather than the message being retried.
/// </summary>
public abstract class TransferStepConsumer<TRequest, TFaulted> : IConsumer<TRequest>
    where TRequest : TransferStepRequestBase
    where TFaulted : TransferStepFaultedBase, new()
{
    private readonly IHubContext<RunnerHub> _hubContext;
    private readonly RunnerSelectionService _runnerSelection;

    protected readonly ILogger _logger;

    protected TransferStepConsumer(
        ILogger logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection)
    {
        _logger = logger;
        _hubContext = hubContext;
        _runnerSelection = runnerSelection;
    }

    /// <summary>The hub method the runner answers this step on.</summary>
    protected abstract string Endpoint { get; }

    /// <summary>The payload the runner receives, built from the step's own request.</summary>
    protected abstract Task<object> BuildPayload(ConsumeContext<TRequest> context, Guid jobId);

    public async Task Consume(ConsumeContext<TRequest> context)
    {
        var msg = context.Message;
        var jobId = msg.CorrelationId;
        var orgId = msg.OrganizationId;
        var task = GetType().Name.Replace("Transfer", "").Replace("Consumer", "");

        try
        {

            var runner = await _runnerSelection.SelectSpecificRunnerAsync(orgId, msg.RunnerId, msg.RunnerInstanceName);
            if (runner == null)
                throw new InvalidOperationException($"No available runners in pool {msg.RunnerId}");

            await _hubContext.Clients.Client(runner.SignalRConnectionId).SendAsync(
                Endpoint, await BuildPayload(context, jobId));

            _logger.LogDebug(
                "Dispatched {Task} to runner {RunnerName} for Module {ModuleId} of job {JobId}",
                task, runner.InstanceName, msg.ModuleId, jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching {Task} for Module {ModuleId} of job {JobId}",
                task, msg.ModuleId, jobId);

            await context.Publish(new TFaulted
            {
                CorrelationId = jobId,
                OrganizationId = orgId,
                ModuleId = msg.ModuleId,
                ErrorMessage = ex.Message,
                StackTrace = ex.StackTrace,
                IsServerSideError = true
            });
        }
    }

}
