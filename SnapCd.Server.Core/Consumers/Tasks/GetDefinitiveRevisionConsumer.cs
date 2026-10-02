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

namespace SnapCd.Server.Core.Consumers.Tasks;

/// <summary>
/// Server-side consumer that receives GetDefinitiveRevision requests and dispatches them to runners via SignalR.
/// </summary>
public abstract class GetDefinitiveRevisionConsumer<TRequested, TFaulted> : IConsumer<TRequested>
    where TRequested : GetDefinitiveRevisionRequestedBase
    where TFaulted : StepFaultedBase, new()
{
    private readonly ILogger _logger;
    private readonly IHubContext<RunnerHub> _hubContext;
    private readonly RunnerSelectionService _runnerSelection;

    protected GetDefinitiveRevisionConsumer(
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

        _logger.LogDebug("Received GetDefinitiveRevision request for job {JobId} in pool {RunnerId}",
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
                new GetDefinitiveRevisionRequest
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
                    SourceType = msg.Declared.SourceType,
                    SourceRevisionType = msg.Declared.SourceRevisionType,
                    SourceUrl = msg.Declared.SourceUrl,
                    SourceRevision = msg.Declared.SourceRevision
                }
            );

            _logger.LogDebug("Dispatched GetDefinitiveRevision request to runner {RunnerName} for job {JobId}",
                runner.InstanceName, jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching GetDefinitiveRevision request for job {JobId}", jobId);
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

public class ApplyGetDefinitiveRevisionConsumer(
    ILogger<ApplyGetDefinitiveRevisionConsumer> logger,
    IHubContext<RunnerHub> hubContext,
    RunnerSelectionService runnerSelection)
    : GetDefinitiveRevisionConsumer<ApplyGetDefinitiveRevisionRequested, ApplyGetDefinitiveRevisionFaulted>(logger, hubContext, runnerSelection)
{
    protected override string Endpoint => nameof(IApplyEndpoints.ApplyGetDefinitiveRevision);
}

public class DestroyGetDefinitiveRevisionConsumer(
    ILogger<DestroyGetDefinitiveRevisionConsumer> logger,
    IHubContext<RunnerHub> hubContext,
    RunnerSelectionService runnerSelection)
    : GetDefinitiveRevisionConsumer<DestroyGetDefinitiveRevisionRequested, DestroyGetDefinitiveRevisionFaulted>(logger, hubContext, runnerSelection)
{
    protected override string Endpoint => nameof(IDestroyEndpoints.DestroyGetDefinitiveRevision);
}
