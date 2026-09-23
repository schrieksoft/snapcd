// SPDX-License-Identifier: LicenseRef-Snap-CD-Source-Available-1.1
// Copyright (c) 2026 Karl Schriek / Schrieksoft.
// No license is granted to use this file, in whole or in part, (a) as training, fine-tuning, retrieval, or
// embedding data for any machine-learning model, or (b) as input to any machine-learning model, agent, or automated
// system for the purpose of producing a derivative work or reimplementation that is not otherwise permitted by the
// Snap CD Source-Available License (including any Competing Product as defined therein). Contact info@snapcd.io
// for terms covering either use.

using MassTransit;
using SnapCd.Server.Core.Events.Steps.Transfer;
using SnapCd.Server.Core.Services;
using SnapCd.Server.Core.Services.Crud.Transfers;

namespace SnapCd.Server.Core.Consumers.Tasks.Transfers;

/// <summary>
/// Pins one Module of a transfer to a runner instance. Each Module picks its own, so the two can
/// run on separate runners with credentials for their own state only.
/// </summary>
public class TransferSelectRunnerInstanceConsumer : IConsumer<TransferSelectRunnerInstanceRequested>
{
    private readonly ILogger<TransferSelectRunnerInstanceConsumer> _logger;
    private readonly RunnerSelectionService _runnerSelection;
    private readonly RunnerJobAuthorizationService _authorizationService;
    private readonly TransferDispatchGate _dispatchGate;

    public TransferSelectRunnerInstanceConsumer(
        ILogger<TransferSelectRunnerInstanceConsumer> logger,
        RunnerSelectionService runnerSelection,
        RunnerJobAuthorizationService authorizationService,
        TransferDispatchGate dispatchGate)
    {
        _logger = logger;
        _runnerSelection = runnerSelection;
        _authorizationService = authorizationService;
        _dispatchGate = dispatchGate;
    }

    public async Task Consume(ConsumeContext<TransferSelectRunnerInstanceRequested> context)
    {
        var msg = context.Message;
        var jobId = msg.CorrelationId;
        var orgId = msg.OrganizationId;

        try
        {
            await _dispatchGate.EnsureDispatchable(msg.ModuleId, orgId);

            await _authorizationService.ValidateRunnerAssignedToModule(msg.RunnerId, msg.ModuleId, orgId);

            var runnerConnection = await _runnerSelection.SelectRunnerInstance(orgId, msg.RunnerId);
            if (runnerConnection == null)
                throw new InvalidOperationException($"No available connections for runner {msg.RunnerId}");

            await context.Publish(new TransferSelectRunnerInstanceCompleted
            {
                CorrelationId = jobId,
                OrganizationId = orgId,
                ModuleId = msg.ModuleId,
                RunnerInstanceName = runnerConnection.InstanceName
            });

            _logger.LogDebug("Pinned Module {ModuleId} of job {JobId} to runner {RunnerName}",
                msg.ModuleId, jobId, runnerConnection.InstanceName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error selecting a runner for Module {ModuleId} of job {JobId}",
                msg.ModuleId, jobId);

            await context.Publish(new TransferSelectRunnerInstanceFaulted
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
