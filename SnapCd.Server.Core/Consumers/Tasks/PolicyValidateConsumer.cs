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
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Contracts.RunnerRequests.HelperClasses;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.StateMachine.Jobs.Utils;

using SnapCd.Server.Core.Events.Steps.Base;

namespace SnapCd.Server.Core.Consumers.Tasks;

/// <summary>
/// Server-side consumer that receives PolicyValidate requests and dispatches them to runners via SignalR.
/// The policy set is filtered here by job kind (EvaluateOn) so the runner only sees policies it must evaluate.
/// </summary>
public abstract class PolicyValidateConsumer<TRequested, TFaulted> : IConsumer<TRequested>
    where TRequested : PolicyValidateRequestedBase
    where TFaulted : StepFaultedBase, new()
{
    private readonly ILogger _logger;
    private readonly IHubContext<RunnerHub> _hubContext;
    private readonly RunnerSelectionService _runnerSelection;

    protected PolicyValidateConsumer(
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

        _logger.LogDebug("Received PolicyValidate request for job {JobId} in pool {RunnerId}",
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
                new PolicyValidateRequestBase
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
                    Policies = PolicyApplicability.For(msg.Declared, msg.IsDestroyJob),
                    IsDestroyJob = msg.IsDestroyJob
                }
            );

            _logger.LogDebug("Dispatched PolicyValidate request to runner {RunnerName} for job {JobId}",
                runner.InstanceName, jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching PolicyValidate request for job {JobId}", jobId);
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

public class ApplyPolicyValidateConsumer(
    ILogger<ApplyPolicyValidateConsumer> logger,
    IHubContext<RunnerHub> hubContext,
    RunnerSelectionService runnerSelection)
    : PolicyValidateConsumer<ApplyPolicyValidateRequested, ApplyPolicyValidateFaulted>(logger, hubContext, runnerSelection)
{
    protected override string Endpoint => RunnerEndpoints.ApplyPolicyValidate;
}

public class DestroyPolicyValidateConsumer(
    ILogger<DestroyPolicyValidateConsumer> logger,
    IHubContext<RunnerHub> hubContext,
    RunnerSelectionService runnerSelection)
    : PolicyValidateConsumer<DestroyPolicyValidateRequested, DestroyPolicyValidateFaulted>(logger, hubContext, runnerSelection)
{
    protected override string Endpoint => RunnerEndpoints.DestroyPolicyValidate;
}
