// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Events.Steps.ManualJobs;
using SnapCd.Server.Core.Services;

namespace SnapCd.Server.Core.Consumers.Tasks.ManualJobs;

/// <summary>
/// Pins a manual job's Module to a runner instance, so every later step of that job reaches the
/// same one. Each job kind names its own events, so the reply routes back to the saga that asked.
/// </summary>
public abstract class ManualSelectRunnerInstanceConsumer<TRequest, TCompleted, TFaulted>(
    ILogger logger,
    RunnerSelectionService runnerSelection,
    RunnerJobAuthorizationService authorizationService)
    : IConsumer<TRequest>
    where TRequest : ManualStepRequestBase
    where TCompleted : ManualStepResponseBase, new()
    where TFaulted : ManualStepFaultedBase, new()
{
    /// <summary>Carries the pinned instance onto the reply, which each kind's event names itself.</summary>
    protected abstract void SetInstanceName(TCompleted completed, string instanceName);

    public async Task Consume(ConsumeContext<TRequest> context)
    {
        var msg = context.Message;
        var jobId = msg.CorrelationId;
        var orgId = msg.OrganizationId;

        try
        {
            await authorizationService.ValidateRunnerAssignedToModule(msg.RunnerId, msg.ModuleId, orgId);

            var runnerConnection = await runnerSelection.SelectRunnerInstance(orgId, msg.RunnerId);
            if (runnerConnection == null)
                throw new InvalidOperationException($"No available connections for runner {msg.RunnerId}");

            var completed = new TCompleted
            {
                CorrelationId = jobId,
                OrganizationId = orgId,
                ModuleId = msg.ModuleId
            };
            SetInstanceName(completed, runnerConnection.InstanceName);

            await context.Publish(completed);

            logger.LogDebug("Pinned Module {ModuleId} of job {JobId} to runner {RunnerName}",
                msg.ModuleId, jobId, runnerConnection.InstanceName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error selecting a runner for Module {ModuleId} of job {JobId}",
                msg.ModuleId, jobId);

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
