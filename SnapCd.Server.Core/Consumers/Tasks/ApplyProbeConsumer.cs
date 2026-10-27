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
using SnapCd.Contracts.RunnerRequests;
using SnapCd.Server.Core.Events.Steps;
using SnapCd.Server.Core.Hubs;
using SnapCd.Server.Core.Services;
using JobMetadata = SnapCd.Contracts.RunnerRequests.HelperClasses.JobMetadata;

namespace SnapCd.Server.Core.Consumers.Tasks;

/// <summary>
/// Server-side consumer that receives Probe requests and dispatches them to runners via SignalR.
///
/// The probe carries no engine flags or hooks: it plans in the checkout the Module's own job is
/// already holding, with the supplied values standing in for an upstream's changed outputs, and
/// writes nothing.
/// </summary>
public class ApplyProbeConsumer : IConsumer<ApplyProbeRequested>
{
    private readonly ILogger<ApplyProbeConsumer> _logger;
    private readonly IHubContext<RunnerHub> _hubContext;
    private readonly RunnerSelectionService _runnerSelection;

    public ApplyProbeConsumer(
        ILogger<ApplyProbeConsumer> logger,
        IHubContext<RunnerHub> hubContext,
        RunnerSelectionService runnerSelection)
    {
        _logger = logger;
        _hubContext = hubContext;
        _runnerSelection = runnerSelection;
    }

    public async Task Consume(ConsumeContext<ApplyProbeRequested> context)
    {
        var msg = context.Message;
        var jobId = msg.CorrelationId;
        var orgId = msg.OrganizationId;
        var runnerId = msg.RunnerId;

        _logger.LogDebug("Received ApplyProbe request for job {JobId} in pool {RunnerId}", jobId, runnerId);

        try
        {
            var runner = await _runnerSelection.SelectSpecificRunnerAsync(
                orgId, runnerId, msg.RunnerInstanceName);

            if (runner == null)
            {
                _logger.LogWarning("No available runners in pool {RunnerId} for job {JobId}", runnerId, jobId);
                throw new InvalidOperationException($"No available runners in pool {runnerId}");
            }

            await _hubContext.Clients.Client(runner.SignalRConnectionId).SendAsync(
                nameof(IApplyEndpoints.ApplyProbe),
                new ProbeRequestBase
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
                    InputsJson = msg.InputsJson,
                    ProbeWaveId = msg.ProbeWaveId
                }
            );

            _logger.LogDebug("Dispatched ApplyProbe request to runner {RunnerName} for job {JobId}",
                runner.InstanceName, jobId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error dispatching ApplyProbe request for job {JobId}", jobId);
            await context.Publish(new ApplyProbeFaulted
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
