// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using SnapCd.Contracts.Endpoints;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using SnapCd.Contracts.Constants;
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Services;

using SnapCd.Server.Core.Events.Steps.Base;
using SnapCd.Server.Core.Views;
using SnapCd.Server.Core.Views.Metadata;
using JobMetadata = SnapCd.Contracts.RunnerRequests.HelperClasses.JobMetadata;

namespace SnapCd.Server.Core.Consumers.Tasks;

/// <summary>
/// Server-side consumer that receives Validate requests and dispatches them to runners via SignalR.
/// Replaces the old runner-side consumer pattern with direct hub invocation.
/// </summary>
public abstract class ValidateConsumer<TRequested, TFaulted> : IConsumer<TRequested>
    where TRequested : ValidateRequestedBase
    where TFaulted : StepFaultedBase, new()
{
    private readonly ILogger _logger;
    private readonly IHubContext<RunnerHub> _hubContext;
    private readonly RunnerSelectionService _runnerSelection;

    protected ValidateConsumer(
        ILogger logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection)
    {
        _logger = logger;
        _hubContext = hubContext;
        _runnerSelection = runnerSelection;
    }

    /// <summary>The runner endpoint this job kind is dispatched to.</summary>
    protected abstract string Endpoint { get; }

    public async Task Consume(ConsumeContext<TRequested> context)
    {
        var msg = context.Message;
        var jobId = msg.CorrelationId;

        var orgId = msg.OrganizationId;
        var runnerId = msg.RunnerId;
        var specificRunner = msg.RunnerInstanceName;

        _logger.LogDebug("Received Validate request for job {JobId} in pool {RunnerId}",
            jobId, runnerId);

        try
        {
            var runner = await _runnerSelection.SelectSpecificRunnerAsync(orgId, runnerId, specificRunner);

            if (runner == null)
            {
                _logger.LogWarning("No available runners in pool {RunnerId} for job {JobId}", runnerId, jobId);
                throw new InvalidOperationException($"No available runners in pool {runnerId}");
            }

            _logger.LogDebug("Selected runner {RunnerName} (ConnectionId: {ConnectionId}) for job {JobId}",
                runner.InstanceName, runner.SignalRConnectionId, jobId);


            // Invoke method on specific runner via SignalR
            await _hubContext.Clients.Client(runner.SignalRConnectionId).SendAsync(
                Endpoint,
                new ValidateRequestBase
                {
                    JobId = jobId,
                    OrganizationId = orgId,
                    Metadata = new JobMetadata
                    {
                        ModuleName = msg.Declared.ModuleName,
                        NamespaceName = msg.Declared.NamespaceName,
                        StackName = msg.Declared.StackName,
                        ModuleId = msg.Declared.ModuleId,
                        SourceSubdirectory = msg.Declared.SourceSubdirectory
                    },
                    Engine = msg.Declared.Engine,
                    ValidateBeforeHook = msg.Declared.ValidateBeforeHook,
                    ValidateAfterHook = msg.Declared.ValidateAfterHook
                }
            );

            _logger.LogDebug("Dispatched Validate request to runner {RunnerName} for job {JobId}",
                runner.InstanceName, jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching Validate request for job {JobId}", jobId);
            await context.Publish(new TFaulted
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

public class ApplyValidateConsumer(
    ILogger<ApplyValidateConsumer> logger,
    IHubContext<RunnerHub> hubContext,
    RunnerSelectionService runnerSelection)
    : ValidateConsumer<ApplyValidateRequested, ApplyValidateFaulted>(logger, hubContext, runnerSelection)
{
    protected override string Endpoint => nameof(IApplyEndpoints.ApplyValidate);
}

public class DestroyValidateConsumer(
    ILogger<DestroyValidateConsumer> logger,
    IHubContext<RunnerHub> hubContext,
    RunnerSelectionService runnerSelection)
    : ValidateConsumer<DestroyValidateRequested, DestroyValidateFaulted>(logger, hubContext, runnerSelection)
{
    protected override string Endpoint => nameof(IDestroyEndpoints.DestroyValidate);
}
